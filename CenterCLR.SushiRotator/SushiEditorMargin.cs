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
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.VisualStudio.Text.Editor;

namespace CenterCLR.SushiRotator
{
    #region SushiRotatorResources
    // Because will be throwing InvalidCastException from internal VS declarated
    // in SushiEditorMargin...
    internal static class SushiRotatorResources
    {
        public static readonly double PixelsPerSecond = 50.0;
        public static readonly Brush Black;
        public static readonly Brush Yellow;
        public static readonly BitmapSource[] Images;

        static SushiRotatorResources()
        {
            Black = new SolidColorBrush(Colors.Black);
            Black.Freeze();
            Yellow = new SolidColorBrush(Colors.Yellow);
            Yellow.Freeze();

            var assembly = typeof(SushiRotatorResources).Assembly;
            Images =
                assembly.
                    GetManifestResourceNames().
                    Where(name => name.EndsWith(".png")).
                    Select(name =>
                    {
                        var image =
                            new PngBitmapDecoder(
                                assembly.GetManifestResourceStream(name),
                                BitmapCreateOptions.None,
                                BitmapCacheOption.Default).
                            Frames[0];
                        image.Freeze();
                        return (BitmapSource)image;
                    }).
                    ToArray();
        }
    }
    #endregion

    /// <summary>
    /// Margin's canvas and visual definition including both size and content
    /// </summary>
    internal sealed class SushiEditorMargin : Canvas, IWpfTextViewMargin
    {
        /// <summary>
        /// Margin name.
        /// </summary>
        public const string MarginName = "SushiEditorMargin";

        /// <summary>
        /// A value indicating whether the object is disposed.
        /// </summary>
        private bool isDisposed;

        private readonly DispatcherTimer dispatcherTimer;
        private readonly Random r = new Random();

        /// <summary>
        /// Initializes a new instance of the <see cref="SushiEditorMargin"/> class for a given <paramref name="textView"/>.
        /// </summary>
        /// <param name="textView">The <see cref="IWpfTextView"/> to attach the margin to.</param>
        public SushiEditorMargin(IWpfTextView textView)
        {
            this.Height = 40;
            this.ClipToBounds = true;
            this.Background = SushiRotatorResources.Black;

            // Setup timer.
            dispatcherTimer =
                new DispatcherTimer(
                    TimeSpan.FromMilliseconds(100),
                    DispatcherPriority.Normal,
                    DispatcherTimer_Tick,
                    this.Dispatcher);

            // Hook ready timing.
            this.Loaded +=
                (sender, args) => dispatcherTimer.Start();
        }

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
                Height = this.Height - 8,
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

        /// <summary>
        /// Timer reached.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DispatcherTimer_Tick(object sender, EventArgs e)
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
            var animation = new DoubleAnimation
            {
                From = width,
                To = 0 - width,
                Duration = TimeSpan.FromMilliseconds(width / SushiRotatorResources.PixelsPerSecond * 1000.0)
            };

            // Create storyboard and relate animation.
            var storyboard = new Storyboard();
            EventHandler completed = null;
            completed = (o, args) =>
            {
                this.Children.Remove(element);
                storyboard.Children.Remove(animation);
                animation.Completed -= completed;
            };
            animation.Completed += completed;

            Storyboard.SetTarget(animation, element);
            Storyboard.SetTargetProperty(animation, new PropertyPath("(Canvas.Left)"));
            storyboard.Children.Add(animation);

            // Start animation.
            storyboard.Begin();

            // Schedule next timeout.
            dispatcherTimer.Interval = TimeSpan.FromMilliseconds(delay);
        }

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
    }
}
