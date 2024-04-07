// ********************************************************************************************************************
//
// CMProgramCanvas.cs : canvas to display countermeasure programs
//
// Copyright(C) 2023 ilominar/raven
//
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU General
// Public License as published by the Free Software Foundation, either version 3 of the License, or (at your
// option) any later version.
//
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the
// implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public License
// for more details.
//
// You should have received a copy of the GNU General Public License along with this program.  If not, see
// <https://www.gnu.org/licenses/>.
//
// ********************************************************************************************************************

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Windows.Foundation;
using Windows.UI;

namespace JAFDTC.UI.Base
{
    /// <summary>
    /// canvas representation of a countermeasure program. this is set up like the viper with salvos and bursts
    /// (where you have N salvos where each salvo is a burst of chaff or flares), countermeasures for other
    /// airframes should be mapped onto this abstraction.
    /// </summary>
    internal class CMProgramCanvasParams
    {
        public bool IsChaff { get; set; }

        public int SQ { get; set; }

        public double SI { get; set; }

        public int BQ { get; set; }

        public double BI { get; set; }

        public CMProgramCanvasParams(bool isChaff = false, int bq = 0, double bi = 0, int sq = 0, double si = 0)
            => (IsChaff, BQ, BI, SQ, SI) = (isChaff, bq, bi, sq, si);
    }

    /// <summary>
    /// canvas to draw a graphical representation of a counter measure program on a timeline showing when the
    /// countermeasures are dispensed.
    /// </summary>
    internal class CMProgramCanvas : Canvas
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // dependency properties
        //
        // ------------------------------------------------------------------------------------------------------------

        DependencyProperty ForegroundProperty = DependencyProperty.Register(
                                                    nameof(Foreground),
                                                    typeof(Brush),
                                                    typeof(CMProgramCanvas),
                                                    new PropertyMetadata(default(Brush),
                                                                         new PropertyChangedCallback(OnForegroundChanged)));
        public Brush Foreground
        {
            get => (Brush)GetValue(ForegroundProperty);
            set => SetValue(ForegroundProperty, value);
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        private CMProgramCanvasParams Pgm { get; set; }

        private double Duration { get; set; }

        private Line CLine { get; set; }

        private List<Line> ChLineSalvoTickList { get; set; }

        private List<TextBlock> ChLineSalvoLabelList { get; set; }

        private List<Shape> ChCounterMarkList { get; set; }

        // ----- private read-only

        private readonly static int _layoMinWidth = 600;

        private readonly static int _layoMarkerSize = 14;
        private readonly static int _layoMarkerStrokeThickness = 1;
        private readonly static int _layoLabelFontSize = 10;
        private readonly static int _layoLabelTickDY = 2;
        private readonly static int _layoTickThickness = 1;
        private readonly static int _layoTickLength = 10;
        private readonly static int _layoLineTickInset = _layoMarkerSize / 2;
        private readonly static int _layoLineY = 8;
        private readonly static int _layoLineThickness = 1;

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public CMProgramCanvas()
        {
            Pgm = new();

            CLine = new()
            {
                Stroke = Foreground, // new SolidColorBrush(Color.FromArgb(0xFF, 0x2F, 0x4F, 0x4F)); // dk slate gray
                StrokeThickness = _layoLineThickness,
                StrokeEndLineCap = PenLineCap.Round
            };
            Children.Add(CLine);

            ChLineSalvoTickList = new List<Line>();
            ChLineSalvoLabelList = new List<TextBlock>();
            ChCounterMarkList = new List<Shape>();
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // dependency property updates
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// changes to the foreground property are propagated into the cached line, tick, and label elements.
        /// </summary>
        private static void OnForegroundChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            CMProgramCanvas canvas = obj as CMProgramCanvas;
            canvas.CLine.Stroke = canvas.Foreground;
            foreach (Line tick in canvas.ChLineSalvoTickList)
            {
                tick.Stroke = canvas.Foreground;
            }
            foreach (TextBlock text in canvas.ChLineSalvoLabelList)
            {
                text.Foreground = canvas.Foreground;
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // methods
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// create and add a line to the canvas to serve as a tick mark. this object will be positioned during the
        /// layout pass. returns the newly-created line.
        /// </summary>
        private Line AddLineSalvoTick()
        {
            Line tick = new()
            {
                Stroke = Foreground,
                StrokeThickness = _layoTickThickness,
                StrokeEndLineCap = PenLineCap.Round
            };
            Children.Add(tick);
            return tick;
        }

        /// <summary>
        /// create and add a text block to the canvas to serve as a tick label. this object will be positioned during
        /// the layout pass. returns the newly-created text block.
        /// </summary>
        /// <returns></returns>
        private TextBlock AddLineSalvoLabel()
        {
            TextBlock block = new()
            {
                Foreground = Foreground,
                FontSize = _layoLabelFontSize
            };
            Children.Add(block);
            return block;
        }

        /// <summary>
        /// create and add a shape to the canvas to serve as a countermeasures marker (circle for flares, diamond for
        /// chaff). returns the newly-created shape.
        /// </summary>
        private Shape AddCounterMarker(bool isChaff)
        {
            Shape marker;
            if (isChaff)
            {
                marker = new Rectangle()
                {
                    Fill = new SolidColorBrush(Color.FromArgb(0xFF, 0x00, 0x80, 0x80)),     // teal
                    Stroke = new SolidColorBrush(Color.FromArgb(0xFF, 0x40, 0x40, 0x40)),   // custom dark gray
                    StrokeThickness = _layoMarkerStrokeThickness,
                    Width = _layoMarkerSize / 1.2,
                    Height = _layoMarkerSize / 1.2,
                    Rotation = 45
                };
            }
            else
            {
                marker = new Ellipse()
                {
                    Fill = new SolidColorBrush(Color.FromArgb(0xFF, 0xDA, 0xA5, 0x20)),     // goldenrod
                    Stroke = new SolidColorBrush(Color.FromArgb(0xFF, 0x40, 0x40, 0x40)),   // custom dark gray
                    StrokeThickness = _layoMarkerStrokeThickness,
                    Width = _layoMarkerSize,
                    Height = _layoMarkerSize
                };
            }
            Children.Add(marker);
            return marker;
        }

        /// <summary>
        /// set up the program being displayed by the canvas and create the necessary tick marks, labels, and markers
        /// needed for display. the time line is scaled according to the longer of the chaff or flare configuration
        /// in the program.
        /// </summary>
        public void SetProgram(CMProgramCanvasParams pgmCanvas, CMProgramCanvasParams pgmOther)
        {
            double durCanvas = 1;
            if (pgmCanvas.SQ * pgmCanvas.SI > pgmOther.SQ * pgmOther.SI)
            {
                durCanvas = pgmCanvas.SQ * pgmCanvas.SI;
            }
            else if (pgmOther.SQ * pgmOther.SI > 0)
            {
                durCanvas = pgmOther.SQ * pgmOther.SI;
            }

            if (Duration != durCanvas ||
                pgmCanvas.BQ != Pgm.BQ || pgmCanvas.BI != Pgm.BI ||
                pgmCanvas.SQ != Pgm.SQ || pgmCanvas.SI != Pgm.SI)
            {
                Duration = durCanvas;
                Pgm = pgmCanvas;

                int nRemove = ChLineSalvoTickList.Count - (Pgm.SQ * Pgm.BQ > 0 ? Pgm.SQ + 1 : 0);
                for (int i = 0; i < nRemove; i++)
                {
                    Children.Remove(ChLineSalvoTickList[0]);
                    ChLineSalvoTickList.RemoveAt(0);
                    Children.Remove(ChLineSalvoLabelList[0]);
                    ChLineSalvoLabelList.RemoveAt(0);
                }
                if (Pgm.SQ * Pgm.BQ > 0)
                {
                    for (int i = ChLineSalvoTickList.Count; i < Pgm.SQ + 1; i++)
                    {
                        ChLineSalvoLabelList.Add(AddLineSalvoLabel());
                        ChLineSalvoTickList.Add(AddLineSalvoTick());
                    }
                    for (int i = 0; i < Pgm.SQ + 1; i++)
                    {
                        ChLineSalvoLabelList[i].Text = (i * Pgm.SI).ToString("0.00");
                    }
                }

                nRemove = ChCounterMarkList.Count - Pgm.SQ * Pgm.BQ;
                for (int i = 0; i < nRemove; i++)
                {
                    Children.Remove(ChCounterMarkList[0]);
                    ChCounterMarkList.RemoveAt(0);
                }
                for (int i = ChCounterMarkList.Count; i < Pgm.SQ * Pgm.BQ; i++)
                {
                    ChCounterMarkList.Add(AddCounterMarker(Pgm.IsChaff));
                }

                InvalidateMeasure();
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // element overrides
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// TODO
        /// </summary>
        protected override Size ArrangeOverride(Size finalSize)
        {
            // TODO: look into the layout, right now, timeline extends all the way to the end of the final salvo
            // TODO: even if the last burst ends long before that point. this can result in a lot of wasted horizontal
            // TODO: space. may make sense to have a more intelligent calculation of the end point that places the
            // TODO: endpoint somewhere in the final salvo if there will be lots of whitespace.

            CLine.X1 = 0; CLine.Y1 = _layoLineY;
            CLine.X2 = finalSize.Width; CLine.Y2 = _layoLineY;

            float dxMarker = Pgm.IsChaff ? 7.0f : 0.0f;

            if (Pgm.BQ * Pgm.SQ > 0)
            {
                double pixPerSec = (finalSize.Width - _layoLineTickInset * 2) / Duration;
                for (int i = 0; i < Pgm.SQ + 1; i++)
                {
                    Line tick = ChLineSalvoTickList[i];
                    tick.X1 = i * Pgm.SI * pixPerSec + _layoLineTickInset; tick.Y1 = CLine.Y1;
                    tick.X2 = i * Pgm.SI * pixPerSec + _layoLineTickInset; tick.Y2 = CLine.Y1 + _layoTickLength;

                    TextBlock label = ChLineSalvoLabelList[i];
                    double labelX = tick.X1 - label.DesiredSize.Width / 2;
                    if (labelX < CLine.X1)
                    {
                        labelX = CLine.X1;
                    }
                    else if (labelX + label.DesiredSize.Width > CLine.X2)
                    {
                        labelX = CLine.X2 - label.DesiredSize.Width;
                    }
                    label.Translation = new((float)labelX, (float)(tick.Y2 + _layoLabelTickDY), 0);

                    if (i < Pgm.SQ)
                    {
                        double tBase = i * Pgm.SI;
                        for (int j = 0; j < Pgm.BQ; j++)
                        {
                            Shape marker = ChCounterMarkList[i * Pgm.BQ + j];
                            marker.Translation = new((float)((tBase + j * Pgm.BI) * pixPerSec) + dxMarker, 0, 1);
                        }
                    }
                }
            }
            return base.ArrangeOverride(finalSize);
        }

        /// <summary>
        /// override the measure method of the element to handle the measure pass. measure each of our children
        /// ("my, how you've grown!"), return the minimum size for the canvas for now, we'll clean it up in layout.
        /// </summary>
        protected override Size MeasureOverride(Size availableSize)
        {
            foreach (UIElement child in Children)
            {
                child.Measure(availableSize);
            }
            double minHeight = _layoMarkerSize + 2 * _layoLabelTickDY + 16;
            return new Size(_layoMinWidth, minHeight);
        }
    }
}
