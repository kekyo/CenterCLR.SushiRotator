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
using System.Windows;
using System.Windows.Media.Animation;

namespace CenterCLR.SushiRotator
{
    internal sealed class DelegatedAnimation
    {
        private DoubleAnimation animation;
        private Storyboard storyboard = new Storyboard();
        private EventHandler handler;

        public DelegatedAnimation(
            UIElement target, PropertyPath path, double begin, double end, double milliseconds)
        {
            // Create animation.
            animation = new DoubleAnimation
            {
                From = begin,
                To = end,
                Duration = TimeSpan.FromMilliseconds(milliseconds)
            };

            // Create storyboard and relate animation.
            handler = (o, args) =>
            {
                this.Completed?.Invoke(this, EventArgs.Empty);
                this.Abort();
            };
            animation.Completed += handler;

            Storyboard.SetTarget(animation, target);
            Storyboard.SetTargetProperty(animation, path);
            storyboard.Children.Add(animation);
        }

        public event EventHandler Completed;

        public void Begin()
        {
            storyboard.Begin();
        }

        public void Abort()
        {
            if (storyboard != null)
            {
                storyboard.Children.Remove(animation);
                animation.Completed -= handler;
                handler = null;
                animation = null;
                storyboard = null;
                this.Completed = null;
            }
        }
    }
}
