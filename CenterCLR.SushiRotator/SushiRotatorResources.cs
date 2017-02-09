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

using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CenterCLR.SushiRotator
{
    // Because will be throwing InvalidCastException from internal VS declarated
    // in SushiEditorMargin... (MEF BUG?)
    internal static class SushiRotatorResources
    {
        public static readonly double CanvasHeight = 40.0;
        public static readonly double CanvasClosingDuration = 150;
        public static readonly double CanvasReopenDuration = 1500;
        public static readonly double CanvasReopenDelay = 5000;
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
}
