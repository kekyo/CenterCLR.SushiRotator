////////////////////////////////////////////////////////////////////////////
// SushiRotator - A reason for aborted working
// Copyright(c) 2016 Kouji Matsui(@kekyo2)
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
////////////////////////////////////////////////////////////////////////////

using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.VisualStudio.Text.Editor;

namespace CenterCLR.SushiRotator
{
    /// <summary>
    /// Margin's canvas and visual definition including both size and content
    /// </summary>
    internal sealed class SushiEditorMargin : Canvas, IWpfTextViewMargin
    {
        private enum ShowingStates
        {
            Closed,
            Opening,
            Opened,
            Closing
        }

        /// <summary>
        /// Margin name.
        /// </summary>
        public const string MarginName = "SushiEditorMargin";

        #region Private fields.
        /// <summary>
        /// A value indicating whether the object is disposed.
        /// </summary>
        private bool isDisposed;

        private readonly DispatcherTimer sushiGeneratorTimer;
        private readonly DispatcherTimer canvasReopenTimer;
        private DelegatedAnimation canvasHeightAnimation;
        private ShowingStates showingState = ShowingStates.Closed;
        private readonly Random r = new Random();
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="SushiEditorMargin"/> class for a given <paramref name="textView"/>.
        /// </summary>
        /// <param name="wpfTextViewHost">The <see cref="IWpfTextViewHost"/> for which to create the <see cref="IWpfTextViewMargin"/>.</param>
        /// <param name="marginContainer">The margin that will contain the newly-created margin.</param>
        public SushiEditorMargin(IWpfTextViewHost wpfTextViewHost, IWpfTextViewMargin marginContainer)
        {
            this.Height = 0;
            this.Visibility = Visibility.Collapsed;
            this.ClipToBounds = true;
            this.Background = SushiRotatorResources.Black;

            var textView = wpfTextViewHost.TextView;
            if (IsValidTextView(textView) == false)
            {
                // Nothing to do for text or code view (exclude output view).
                return;
            }

            // Setup timers.
            sushiGeneratorTimer =
                new DispatcherTimer(
                    TimeSpan.FromMilliseconds(1000),
                    DispatcherPriority.Normal,
                    this.SushiGeneratorTimer_Tick,
                    this.Dispatcher);
            canvasReopenTimer =
                new DispatcherTimer(
                    TimeSpan.FromMilliseconds(SushiRotatorResources.CanvasReopenDelay),
                    DispatcherPriority.Normal,
                    this.CanvasReopenTimer_Tick,
                    this.Dispatcher);

            // Hook ready timing.
            this.Loaded += (sender, args) =>
            {
                sushiGeneratorTimer.Start();
                canvasReopenTimer.Start();
            };

            textView.GotAggregateFocus += (s, e) => this.CloseSushiBar();
            textView.LostAggregateFocus += (s, e) => this.OpenSushiBar();

            textView.ViewportWidthChanged += (s, e) => this.CloseSushiBar();
            textView.ViewportLeftChanged += (s, e) => this.CloseSushiBar();
            textView.ZoomLevelChanged += (s, e) => this.CloseSushiBar();
            textView.MouseHover += (s, e) => this.CloseSushiBar();
            textView.TextBuffer.Changed += (s, e) => this.CloseSushiBar();

            textView.Caret.PositionChanged += (s, e) => this.CloseSushiBar();
        }
        #endregion

        #region IsValidTextView
        /// <summary>
        /// Compute target text view.
        /// </summary>
        /// <param name="textView">IWpfTextView</param>
        /// <returns>True if valid text view.</returns>
        private static bool IsValidTextView(IWpfTextView textView)
        {
            return
                // Exclude output view.
                (textView.TextBuffer.ContentType.TypeName.IndexOf(
                    "output", StringComparison.InvariantCultureIgnoreCase) == -1) &&
                // Exclude subtype is not text/code.
                textView.TextBuffer.ContentType.BaseTypes.Any(ct =>
                    ct.TypeName.Equals("text", StringComparison.InvariantCultureIgnoreCase) ||
                    ct.TypeName.Equals("code", StringComparison.InvariantCultureIgnoreCase));
        }
        #endregion

        #region Generate sushi image
        /// <summary>
        /// Generate funny sushi image.
        /// </summary>
        /// <returns>Image</returns>
        private FrameworkElement GenerateSushi()
        {
            var index = r.Next(SushiRotatorResources.Images.Length - 1);
            var image = new Image
            {
                Source = SushiRotatorResources.Images[index],
                Height = SushiRotatorResources.CanvasHeight - 8,
                Stretch = Stretch.Uniform
            };

            // Apply reality :)
            RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.HighQuality);

            Canvas.SetLeft(image, this.ActualWidth);
            Canvas.SetTop(image, 4);

            return image;
        }

        private void ForceCalculate()
        {
            // DIRTY: Force calculated element size.
            var d = (UIElement)this;
            while (true)
            {
                var p = VisualTreeHelper.GetParent(d) as UIElement;
                if (p == null)
                {
                    break;
                }
                d = p;
            }

            d.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            d.Arrange(new Rect(0, 0, d.DesiredSize.Width, d.DesiredSize.Height));
        }

        protected override void OnVisualParentChanged(DependencyObject oldParent)
        {
            if (sushiGeneratorTimer != null)
            {
                // DIRTY: Schedule delay.
                //   If view is undocks from VS window, ForceCalculate will fails traverse parents.
                sushiGeneratorTimer.Interval = TimeSpan.FromMilliseconds(1000);
                sushiGeneratorTimer.Start();
            }

            base.OnVisualParentChanged(oldParent);
        }

        /// <summary>
        /// Timer reached (Generate sushi)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SushiGeneratorTimer_Tick(object sender, EventArgs e)
        {
            // Generate sushi.
            var element = this.GenerateSushi();
            this.Children.Add(element);

            this.ForceCalculate();

            // Calculate next timeout.
            var delay =
                r.Next(3000) +
                ((element.ActualWidth * 1.5) / SushiRotatorResources.PixelsPerSecond * 1000.0);

            // Create animation.
            var width = this.ActualWidth + 10;
            var sushiRotateAnimation = new DelegatedAnimation(
                element,
                new PropertyPath("(Canvas.Left)"),
                width,
                0 - width,
                width / SushiRotatorResources.PixelsPerSecond * 1000.0);
            sushiRotateAnimation.Completed += (s, e2) => this.Children.Remove(element);

            // Start animation.
            sushiRotateAnimation.Begin();

            // Schedule next timeout.
            sushiGeneratorTimer.Interval = TimeSpan.FromMilliseconds(delay);
            sushiGeneratorTimer.Start();
        }
        #endregion

        #region Canvas close/reopen
        /// <summary>
        /// Abort canvas animation.
        /// </summary>
        /// <returns>Current canvas height.</returns>
        private double AbortSushiBarAnimation()
        {
            var currentHeight = this.ActualHeight;
            canvasHeightAnimation?.Abort();
            return currentHeight;
        }

        /// <summary>
        /// Close canvas.
        /// </summary>
        private void CloseSushiBar()
        {
            // Finish current animation.
            var currentHeight = this.AbortSushiBarAnimation();

            Debug.WriteLine("CloseSushiBar: Begin: " + showingState);

            canvasHeightAnimation = new DelegatedAnimation(
                this,
                new PropertyPath("(Canvas.Height)"),
                currentHeight,
                0,
                SushiRotatorResources.CanvasClosingDuration / SushiRotatorResources.CanvasHeight * currentHeight);

            canvasHeightAnimation.Completed += (s, e) =>
            {
                Debug.WriteLine("CloseSushiBar: Finished: " + showingState);

                if (showingState != ShowingStates.Closing)
                {
                    this.Visibility = Visibility.Collapsed;
                }
                showingState = ShowingStates.Closed;

                // Start reopen timer.
                canvasReopenTimer.Interval =
                    TimeSpan.FromMilliseconds(SushiRotatorResources.CanvasReopenDelay);
                canvasReopenTimer.Start();
            };

            // Start animation.
            showingState = ShowingStates.Closing;
            canvasHeightAnimation.Begin();
        }

        /// <summary>
        /// Open canvas.
        /// </summary>
        private void OpenSushiBar()
        {
            // Finish current animation.
            var currentHeight = this.AbortSushiBarAnimation();

            Debug.WriteLine("OpenSushiBar: Begin: " + showingState);

            canvasHeightAnimation = new DelegatedAnimation(
                this,
                new PropertyPath("(Canvas.Height)"),
                currentHeight,
                SushiRotatorResources.CanvasHeight,
                SushiRotatorResources.CanvasReopenDuration / SushiRotatorResources.CanvasHeight * (SushiRotatorResources.CanvasHeight - currentHeight));

            canvasHeightAnimation.Completed += (s, e) =>
            {
                Debug.WriteLine("OpenSushiBar: Finished: " + showingState);
                showingState = ShowingStates.Opened;
            };

            // Start animation.
            this.Visibility = Visibility.Visible;
            showingState = ShowingStates.Opening;
            canvasHeightAnimation.Begin();

            // Stop reopen timer.
            canvasReopenTimer.Stop();
        }

        /// <summary>
        /// Timer reached (Reopen canvas)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CanvasReopenTimer_Tick(object sender, EventArgs e)
        {
            this.OpenSushiBar();
        }
        #endregion

        #region IWpfTextViewMargin
        /// <summary>
        /// Gets the <see cref="FrameworkElement"/> that implements the visual representation of the margin.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The margin is disposed.</exception>
        public FrameworkElement VisualElement
        {
            // Since this margin implements Canvas, this is the object which renders
            // the margin.
            get
            {
                this.ThrowIfDisposed();
                return this;
            }
        }
        #endregion

        #region ITextViewMargin
        /// <summary>
        /// Gets the size of the margin.
        /// </summary>
        /// <remarks>
        /// For a horizontal margin this is the height of the margin,
        /// since the width will be determined by the <see cref="ITextView"/>.
        /// For a vertical margin this is the width of the margin,
        /// since the height will be determined by the <see cref="ITextView"/>.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">The margin is disposed.</exception>
        public double MarginSize
        {
            get
            {
                this.ThrowIfDisposed();

                // Since this is a horizontal margin, its width will be bound to the width of the text view.
                // Therefore, its size is its height.
                return this.ActualHeight;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the margin is enabled.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The margin is disposed.</exception>
        public bool Enabled
        {
            get
            {
                this.ThrowIfDisposed();

                // The margin should always be enabled
                return true;
            }
        }

        /// <summary>
        /// Gets the <see cref="ITextViewMargin"/> with the given <paramref name="marginName"/> or null if no match is found
        /// </summary>
        /// <param name="marginName">The name of the <see cref="ITextViewMargin"/></param>
        /// <returns>The <see cref="ITextViewMargin"/> named <paramref name="marginName"/>, or null if no match is found.</returns>
        /// <remarks>
        /// A margin returns itself if it is passed its own name. If the name does not match and it is a container margin, it
        /// forwards the call to its children. Margin name comparisons are case-insensitive.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="marginName"/> is null.</exception>
        public ITextViewMargin GetTextViewMargin(string marginName)
        {
            return string.Equals(marginName, SushiEditorMargin.MarginName, StringComparison.OrdinalIgnoreCase) ? this : null;
        }

        /// <summary>
        /// Disposes an instance of <see cref="SushiEditorMargin"/> class.
        /// </summary>
        public void Dispose()
        {
            if (!this.isDisposed)
            {
                GC.SuppressFinalize(this);
                this.isDisposed = true;
            }
        }
        #endregion

        #region ThrowIfDisposed
        /// <summary>
        /// Checks and throws <see cref="ObjectDisposedException"/> if the object is disposed.
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (this.isDisposed)
            {
                throw new ObjectDisposedException(MarginName);
            }
        }
        #endregion
    }
}
