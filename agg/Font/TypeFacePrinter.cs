﻿//----------------------------------------------------------------------------
// Anti-Grain Geometry - Version 2.4
//
// C# Port port by: Lars Brubaker
//                  larsbrubaker@gmail.com
// Copyright (C) 2007-2011
//
// Permission to copy, use, modify, sell and distribute this software 
// is granted provided this copyright notice appears in all copies. 
// This software is provided "as is" without express or implied
// warranty, and with no claim as to its suitability for any purpose.
//
//----------------------------------------------------------------------------
//
// Class StringPrinter.cs
// 
// Class to output the vertex source of a string as a run of glyphs.
//----------------------------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

using MatterHackers.Agg;
using MatterHackers.Agg.Image;
using MatterHackers.Agg.Transform;
using MatterHackers.Agg.VertexSource;
using MatterHackers.VectorMath;

namespace MatterHackers.Agg.Font
{
    public enum Justification { Left, Center, Right };
    public enum Baseline { BoundsTop, BoundsCenter, TextCenter, Text, BoundsBottom };

    public class TypeFacePrinter : IVertexSource
    {
        StyledTypeFace typeFaceStyle;

        String text = "";

        Vector2 totalSizeCach;

        public Justification Justification { get; set; }
        public Baseline Baseline { get; set; }

        public bool DrawFromHintedCache { get; set; }

        public StyledTypeFace TypeFaceStyle
        {
            get
            {
                return typeFaceStyle;
            }
        }

        public String Text
        {
            get
            {
                return text;
            }
            set
            {
                if (text != value)
                {
                    totalSizeCach.x = 0;
                    text = value;
                }
            }
        }

        public Vector2 Origin { get; set; }


        public TypeFacePrinter(String text = "", double pointSize = 12, Vector2 origin = new Vector2(), Justification justification = Justification.Left, Baseline baseline = Baseline.Text)
            : this(text, new StyledTypeFace(LiberationSansFont.Instance, pointSize), origin, justification, baseline)
        {
        }
        
        public TypeFacePrinter(String text, StyledTypeFace typeFaceStyle, Vector2 origin = new Vector2(), Justification justification = Justification.Left, Baseline baseline = Baseline.Text)
        {
            this.typeFaceStyle = typeFaceStyle;
            this.text = text;
            this.Justification = justification;
            this.Origin = origin;
            this.Baseline = baseline;
        }

        public TypeFacePrinter(String text, TypeFacePrinter copyPropertiesFrom)
            : this(text, copyPropertiesFrom.TypeFaceStyle, copyPropertiesFrom.Origin, copyPropertiesFrom.Justification, copyPropertiesFrom.Baseline)
        {
        }

        public static string InsertCRs(string textToWrap, double maxPixelWidth, double pointSize)
        {
            StringBuilder textWithCRs = new StringBuilder();
            string[] lines = WrapText(textToWrap, maxPixelWidth, pointSize);
            for (int i = 0; i < lines.Length; i++ )
            {
                string line = lines[i];
                if (i == 0)
                {
                    textWithCRs.Append(line);
                }
                else
                {
                    textWithCRs.Append("\n" + line);
                }
            }

            return textWithCRs.ToString();
        }

        public static string[] WrapText(string originalTextToWrap, double maxPixelWidth, double pointSize)
        {
            List<string> finalLines = new List<string>();
            string[] splitOnNL = originalTextToWrap.Split('\n');
            foreach (string line in splitOnNL)
            {
                string[] linesFromWidth = WrapSingleLineOnWidth(line, maxPixelWidth, pointSize);
                if (linesFromWidth.Length == 0)
                {
                    finalLines.Add("");
                }
                else
                {
                    finalLines.AddRange(linesFromWidth);
                }
            }

            return finalLines.ToArray();
        }

        static string[] WrapSingleLineOnWidth(string originalTextToWrap, double maxPixelWidth, double pointSize)
        {
            string textToWrap = originalTextToWrap;
            List<string> lines = new List<string>();
            while (textToWrap.Length > 0)
            {
                TypeFacePrinter printer = new TypeFacePrinter(textToWrap, pointSize);
                int remainingLength = textToWrap.Length;
                while (printer.GetSize().x > maxPixelWidth)
                {
                    remainingLength--;
                    while (textToWrap.Substring(0, remainingLength).Contains(" ") && remainingLength > 1 && textToWrap[remainingLength] != ' ')
                    {
                        remainingLength--;
                    }
                    printer.Text = textToWrap.Substring(0, remainingLength);
                }

                lines.Add(textToWrap.Substring(0, remainingLength));

                // check if we wrapped because of to long or a '\n'. If '\n' we only trim a leading space if to long.
                if (remainingLength > 1 // we have more than 2 charecters left
                    && textToWrap.Length > remainingLength // we are longer than the remaining text
                    && textToWrap[remainingLength] == ' ' // the first new character is a space
                    && textToWrap[remainingLength - 1] != '\n') // the character before the space was not a cr (wrapped because of length)
                {
                    textToWrap = textToWrap.Substring(remainingLength+1);
                }
                else
                {
                    textToWrap = textToWrap.Substring(remainingLength);
                }
            }

            return lines.ToArray();
        }

        public RectangleDouble LocalBounds
        {
            get
            {
                Vector2 size = GetSize();
                RectangleDouble bounds;

                switch (Justification)
                {
                    case Justification.Left:
                        bounds = new RectangleDouble(0, typeFaceStyle.DescentInPixels, size.x, size.y + typeFaceStyle.DescentInPixels);
                        break;

                    case Justification.Center:
                        bounds = new RectangleDouble(-size.x / 2, typeFaceStyle.DescentInPixels, size.x / 2, size.y + typeFaceStyle.DescentInPixels);
                        break;

                    case Justification.Right:
                        bounds = new RectangleDouble(-size.x, typeFaceStyle.DescentInPixels, 0, size.y + typeFaceStyle.DescentInPixels);
                        break;

                    default:
                        throw new NotImplementedException();
                }

                switch (Baseline)
                {
                    case Font.Baseline.BoundsCenter:
                        bounds.Offset(0, -bounds.Height / 2);
                        break;

                    default:
                        break;
                }

                bounds.Offset(Origin);
                return bounds;
            }
        }

        public void Render(Graphics2D graphics2D, RGBA_Bytes color, IVertexSourceProxy vertexSourceToApply)
        {
            vertexSourceToApply.VertexSource = this;
            rewind(0);
            if (DrawFromHintedCache)
            {
                // TODO: make this work
                graphics2D.Render(vertexSourceToApply, color);
            }
            else
            {
                graphics2D.Render(vertexSourceToApply, color);
            }
        }

        public void Render(Graphics2D graphics2D, RGBA_Bytes color)
        {
            rewind(0);
            if (DrawFromHintedCache)
            {
                RenderFromCache(graphics2D, color);
            }
            else
            {
                graphics2D.Render(this, color);
            }
        }

        void RenderFromCache(Graphics2D graphics2D, RGBA_Bytes color)
        {
            if (text != null && text.Length > 0)
            {
                Vector2 currentOffset = Vector2.Zero;

                currentOffset = GetBaseline(currentOffset);
                currentOffset.y += Origin.y;

                string[] lines = text.Split('\n');
                foreach (string line in lines)
                {
                    currentOffset = GetXPositionForLineBasedOnJustification(currentOffset, line);
                    currentOffset.x += Origin.x;

                    for (int currentChar = 0; currentChar < line.Length; currentChar++)
                    {
                        ImageBuffer currentGlyphImage = typeFaceStyle.GetImageForCharacter(line[currentChar], 0, 0);

                        if (currentGlyphImage != null)
                        {
                            graphics2D.Render(currentGlyphImage, currentOffset);
                        }

                        // get the advance for the next character
                        if (currentChar < line.Length - 1)
                        {
                            // pass the next char so the typeFaceStyle can do kerning if it needs to.
                            currentOffset.x += typeFaceStyle.GetAdvanceForCharacter(line[currentChar], line[currentChar + 1]);
                        }
                        else
                        {
                            currentOffset.x += typeFaceStyle.GetAdvanceForCharacter(line[currentChar]);
                        }
                    }

                    // before we go onto the next line we need to move down a line
                    currentOffset.x = 0;
                    currentOffset.y -= typeFaceStyle.EmSizeInPixels;
                }
            }
        }

        public IEnumerable<VertexData> VertexIterator()
        {
            if (text != null && text.Length > 0)
            {
                Vector2 currentOffset = new Vector2(0, 0);

                currentOffset = GetBaseline(currentOffset);

                string[] lines = text.Split('\n');
                foreach (string line in lines)
                {
                    currentOffset = GetXPositionForLineBasedOnJustification(currentOffset, line);

                    for (int currentChar = 0; currentChar < line.Length; currentChar++)
                    {
                        IVertexSource currentGlyph = typeFaceStyle.GetGlyphForCharacter(line[currentChar]);

                        if (currentGlyph != null)
                        {
                            foreach (VertexData vertexData in currentGlyph.VertexIterator())
                            {
                                if (vertexData.command != ShapePath.FlagsAndCommand.CommandStop)
                                {
                                    VertexData offsetVertex = new VertexData(vertexData.command, vertexData.position + currentOffset + Origin);
                                    yield return offsetVertex;
                                }
                            }
                        }

                        // get the advance for the next character
                        if (currentChar < line.Length - 1)
                        {
                            // pass the next char so the typeFaceStyle can do kerning if it needs to.
                            currentOffset.x += typeFaceStyle.GetAdvanceForCharacter(line[currentChar], line[currentChar + 1]);
                        }
                        else
                        {
                            currentOffset.x += typeFaceStyle.GetAdvanceForCharacter(line[currentChar]);
                        }
                    }

                    // before we go onto the next line we need to move down a line
                    currentOffset.x = 0;
                    currentOffset.y -= typeFaceStyle.EmSizeInPixels;
                }
            }

            VertexData endVertex = new VertexData(ShapePath.FlagsAndCommand.CommandStop, Vector2.Zero);
            yield return endVertex;
        }

        private Vector2 GetXPositionForLineBasedOnJustification(Vector2 currentOffset, string line)
        {
            Vector2 size = GetSize(line);
            switch (Justification)
            {
                case Justification.Left:
                    currentOffset.x = 0;
                    break;

                case Justification.Center:
                    currentOffset.x = -size.x / 2;
                    break;

                case Justification.Right:
                    currentOffset.x = -size.x;
                    break;

                default:
                    throw new NotImplementedException();
            }
            return currentOffset;
        }

        private Vector2 GetBaseline(Vector2 currentOffset)
        {
            switch (Baseline)
            {
                case Baseline.Text:
                    currentOffset.y = 0;
                    break;

                case Baseline.BoundsTop:
                    currentOffset.y = -typeFaceStyle.AscentInPixels;
                    break;

                case Baseline.BoundsCenter:
                    currentOffset.y = -typeFaceStyle.AscentInPixels / 2;
                    break;

                default:
                    throw new NotImplementedException();
            }
            return currentOffset;
        }

#if true
        IEnumerator<VertexData> currentEnumerator;
        public void rewind(int layerIndex)
        {
            currentEnumerator = VertexIterator().GetEnumerator();
            currentEnumerator.MoveNext();
        }

        public ShapePath.FlagsAndCommand vertex(out double x, out double y)
        {
            x = currentEnumerator.Current.position.x;
            y = currentEnumerator.Current.position.y;
            ShapePath.FlagsAndCommand command = currentEnumerator.Current.command;

            currentEnumerator.MoveNext();

            return command;
        }
#else
        public void rewind(int pathId)
        {
            currentChar = 0;
            currentOffset = new Vector2(0, 0);
            if (text != null && text.Length > 0)
            {
                currentGlyph = typeFaceStyle.GetGlyphForCharacter(text[currentChar]);
                if (currentGlyph != null)
                {
                    currentGlyph.rewind(0);
                }
            }
        }

        public ShapePath.FlagsAndCommand vertex(out double x, out double y)
        {
            x = 0;
            y = 0;
            if (text != null && text.Length > 0)
            {
                ShapePath.FlagsAndCommand curCommand = ShapePath.FlagsAndCommand.CommandStop;
                if (currentGlyph != null)
                {
                    curCommand = currentGlyph.vertex(out x, out y);
                }

                double xAlignOffset = 0;
                Vector2 size = GetSize();
                switch (Justification)
                {
                    case Justification.Left:
                        xAlignOffset = 0;
                        break;

                    case Justification.Center:
                        xAlignOffset = -size.x / 2;
                        break;

                    case Justification.Right:
                        xAlignOffset = -size.x;
                        break;

                    default:
                        throw new NotImplementedException();
                }

                double yAlignOffset = 0;
                switch (Baseline)
                {
                    case Baseline.Text:
                        //yAlignOffset = -typeFaceStyle.DescentInPixels;
                        yAlignOffset = 0;
                        break;

                    case Baseline.BoundsTop:
                        yAlignOffset = -typeFaceStyle.AscentInPixels;
                        break;

                    case Baseline.BoundsCenter:
                        yAlignOffset = -typeFaceStyle.AscentInPixels / 2;
                        break;

                    default:
                        throw new NotImplementedException();
                }


                while (curCommand == ShapePath.FlagsAndCommand.CommandStop
                    && currentChar < text.Length - 1)
                {
                    if (currentChar == 0 && text[currentChar] == '\n')
                    {
                        currentOffset.x = 0;
                        currentOffset.y -= typeFaceStyle.EmSizeInPixels;
                    }
                    else
                    {
                        if (currentChar < text.Length)
                        {
                            // pass the next char so the typeFaceStyle can do kerning if it needs to.
                            currentOffset.x += typeFaceStyle.GetAdvanceForCharacter(text[currentChar], text[currentChar + 1]);
                        }
                        else
                        {
                            currentOffset.x += typeFaceStyle.GetAdvanceForCharacter(text[currentChar]);
                        }
                    }

                    currentChar++;
                    currentGlyph = typeFaceStyle.GetGlyphForCharacter(text[currentChar]);
                    if (currentGlyph != null)
                    {
                        currentGlyph.rewind(0);
                        curCommand = currentGlyph.vertex(out x, out y);
                    }
                    else if (text[currentChar] == '\n')
                    {
                        if (currentChar + 1 < text.Length - 1 && (text[currentChar + 1] == '\n') && text[currentChar] != text[currentChar + 1])
                        {
                            currentChar++;
                        }
                        currentOffset.x = 0;
                        currentOffset.y -= typeFaceStyle.EmSizeInPixels;
                    }
                }

                if (ShapePath.is_vertex(curCommand))
                {

                    x += currentOffset.x + xAlignOffset + Origin.x;
                    y += currentOffset.y + yAlignOffset + Origin.y;

                }

                return curCommand;
            }

            return ShapePath.FlagsAndCommand.CommandStop;
        }
#endif

        public Vector2 GetSize(string text = null)
        {
            if (text == null)
            {
                text = this.text;
            }

            if(text != this.text)
            {
                Vector2 calculatedSize;
                GetSize(0, Math.Max(0, text.Length - 1), out calculatedSize, text);
                return calculatedSize;
            }

            if (totalSizeCach.x == 0)
            {
                Vector2 calculatedSize;
                GetSize(0, Math.Max(0, text.Length - 1), out calculatedSize, text);
                totalSizeCach = calculatedSize;
            }

            return totalSizeCach;
        }

        public void GetSize(int characterToMeasureStartIndexInclusive, int characterToMeasureEndIndexInclusive, out Vector2 offset, string text = null)
        {
            if (text == null)
            {
                text = this.text;
            }

            offset.x = 0;
            offset.y = typeFaceStyle.EmSizeInPixels;

            double currentLineX = 0;

            for (int i = characterToMeasureStartIndexInclusive; i < characterToMeasureEndIndexInclusive; i++)
            {
                if (text[i] == '\n')
                {
                    if (i + 1 < characterToMeasureEndIndexInclusive && (text[i + 1] == '\n') && text[i] != text[i+1])
                    {
                        i++;
                    }
                    currentLineX = 0;
                    offset.y += typeFaceStyle.EmSizeInPixels;
                }
                else
                {
                    if (i + 1 < text.Length)
                    {
                        currentLineX += typeFaceStyle.GetAdvanceForCharacter(text[i], text[i + 1]);
                    }
                    else
                    {
                        currentLineX += typeFaceStyle.GetAdvanceForCharacter(text[i]);
                    }
                    if (currentLineX > offset.x)
                    {
                        offset.x = currentLineX;
                    }
                }
            }

            if (text.Length > characterToMeasureEndIndexInclusive)
            {
                if (text[characterToMeasureEndIndexInclusive] == '\n')
                {
                    currentLineX = 0;
                    offset.y += typeFaceStyle.EmSizeInPixels;
                }
                else
                {
                    offset.x += typeFaceStyle.GetAdvanceForCharacter(text[characterToMeasureEndIndexInclusive]);
                }
            }
        }

        public int NumLines()
        {
            int characterToMeasureStartIndexInclusive = 0;
            int characterToMeasureEndIndexInclusive = text.Length - 1;
            return NumLines(characterToMeasureStartIndexInclusive, characterToMeasureEndIndexInclusive);
        }

        public int NumLines(int characterToMeasureStartIndexInclusive, int characterToMeasureEndIndexInclusive)
        {
            int numLines = 1;
            
            characterToMeasureStartIndexInclusive = Math.Max(0, Math.Min(characterToMeasureStartIndexInclusive, text.Length - 1));
            characterToMeasureEndIndexInclusive = Math.Max(0, Math.Min(characterToMeasureEndIndexInclusive, text.Length - 1));
            for (int i = characterToMeasureStartIndexInclusive; i < characterToMeasureEndIndexInclusive; i++)
            {
                if (text[i] == '\n')
                {
                    if (i + 1 < characterToMeasureEndIndexInclusive && (text[i + 1] == '\n') && text[i] != text[i + 1])
                    {
                        i++;
                    }
                    numLines++;
                }
            }

            return numLines;
        }

        public void GetOffset(int characterToMeasureStartIndexInclusive, int characterToMeasureEndIndexInclusive, out Vector2 offset)
        {
            offset = Vector2.Zero;

            characterToMeasureEndIndexInclusive = Math.Min(text.Length-1, characterToMeasureEndIndexInclusive);

            for (int index = characterToMeasureStartIndexInclusive; index <= characterToMeasureEndIndexInclusive; index++)
            {
                if (text[index] == '\n')
                {
                    offset.x = 0;
                    offset.y -= typeFaceStyle.EmSizeInPixels;
                }
                else
                {
                    if (index < text.Length - 1)
                    {
                        offset.x += typeFaceStyle.GetAdvanceForCharacter(text[index], text[index + 1]);
                    }
                    else
                    {
                        offset.x += typeFaceStyle.GetAdvanceForCharacter(text[index]);
                    }
                }
            }
        }

        // this will return the position to the left of the requested character.
        public Vector2 GetOffsetLeftOfCharacterIndex(int characterIndex)
        {
            Vector2 offset;
            GetOffset(0, characterIndex - 1, out offset);
            return offset;
        }

        // If the Text is "TEXT" and the position is less than half the distance to the center
        // of "T" the return value will be 0 if it is between the center of 'T' and the center of 'E'
        // it will be 1 and so on.
        public int GetCharacterIndexToStartBefore(Vector2 position)
        {
            int clostestIndex = -1;
            double clostestXDistSquared = double.MaxValue;
            double clostestYDistSquared = double.MaxValue;
            Vector2 offset = new Vector2(0, typeFaceStyle.EmSizeInPixels * NumLines());
            int characterToMeasureStartIndexInclusive = 0;
            int characterToMeasureEndIndexInclusive = text.Length - 1;
            if (text.Length > 0)
            {
                characterToMeasureStartIndexInclusive = Math.Max(0, Math.Min(characterToMeasureStartIndexInclusive, text.Length - 1));
                characterToMeasureEndIndexInclusive = Math.Max(0, Math.Min(characterToMeasureEndIndexInclusive, text.Length - 1));
                for (int i = characterToMeasureStartIndexInclusive; i <= characterToMeasureEndIndexInclusive; i++)
                {
                    CheckForBetterClickPosition(ref position, ref clostestIndex, ref clostestXDistSquared, ref clostestYDistSquared, ref offset, i);

                    if(text[i] == '\r')
                    {
                        throw new Exception("All \\r's should have been converted to \\n's.");
                    }

                    if (text[i] == '\n')
                    {
                        offset.x = 0;
                        offset.y -= typeFaceStyle.EmSizeInPixels;
                    }
                    else
                    {
                        Vector2 nextSize;
                        GetOffset(i, i, out nextSize);

                        offset.x += nextSize.x;
                    }
                }

                CheckForBetterClickPosition(ref position, ref clostestIndex, ref clostestXDistSquared, ref clostestYDistSquared, ref offset, characterToMeasureEndIndexInclusive + 1);
            }

            return clostestIndex;
        }

        private static void CheckForBetterClickPosition(ref Vector2 position, ref int clostestIndex, ref double clostestXDistSquared, ref double clostestYDistSquared, ref Vector2 offset, int i)
        {
            Vector2 delta = position - offset;
            double deltaYLengthSquared = delta.y * delta.y;
            if (deltaYLengthSquared < clostestYDistSquared)
            {
                clostestYDistSquared = deltaYLengthSquared;
                clostestXDistSquared = delta.x * delta.x;
                clostestIndex = i;
            }
            else if (deltaYLengthSquared == clostestYDistSquared)
            {
                double deltaXLengthSquared = delta.x * delta.x;
                if (deltaXLengthSquared < clostestXDistSquared)
                {
                    clostestXDistSquared = deltaXLengthSquared;
                    clostestIndex = i;
                }
            }
        }
    }
}
