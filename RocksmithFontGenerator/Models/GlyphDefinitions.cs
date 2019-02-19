using System.Collections.Generic;
using System.Windows;
using System.Xml;
using System.Xml.Serialization;

namespace RocksmithFontGenerator.Models
{
    [XmlRoot(Namespace = "")]
    public sealed class GlyphDefinitions
    {
        [XmlAttribute]
        public int TextureWidth { get; set; }

        [XmlAttribute]
        public int TextureHeight { get; set; }

        [XmlElement("GlyphDefinition")]
        public List<GlyphDefinition> Glyphs { get; set; }

        public static void Save(string filename, RS2014FontGenerator fontGenerator)
        {
            int textureWidth = fontGenerator.TextureWidth;
            int textureHeight = fontGenerator.TextureHeight;
            Dictionary<string, Rect> outerRects = fontGenerator.OuterRects;
            Dictionary<string, Rect> innerRects = fontGenerator.InnerRects;

            GlyphDefinitions definitionFile = new GlyphDefinitions
            {
                TextureHeight = textureHeight,
                TextureWidth = textureWidth,
                Glyphs = new List<GlyphDefinition>(fontGenerator.Glyphs.Count)
            };

            foreach(string glyph in fontGenerator.Glyphs)
            {
                Rect innerRect = innerRects[glyph];
                Rect outerRect = outerRects[glyph];

                definitionFile.Glyphs.Add(
                    new GlyphDefinition
                    {
                        Symbol = glyph,

                        InnerXMin = (float)(innerRect.Left / textureWidth),
                        InnerXMax = (float)(innerRect.Right / textureWidth),
                        InnerYMin = (float)(innerRect.Top / textureHeight),
                        InnerYMax = (float)(innerRect.Bottom / textureHeight),

                        OuterXMin = (float)(outerRect.Left / textureWidth),
                        OuterXMax = (float)(outerRect.Right / textureWidth),
                        OuterYMin = (float)(outerRect.Top / textureHeight),
                        OuterYMax = (float)(outerRect.Bottom / textureHeight)
                    });
            }

            XmlHelper.Serialize(filename, definitionFile);
        }
    }

    [XmlRoot(Namespace = "")]
    public sealed class GlyphDefinition
    {
        [XmlAttribute]
        public string Symbol { get; set; }

        [XmlAttribute]
        public float InnerYMin { get; set; }

        [XmlAttribute]
        public float InnerYMax { get; set; }

        [XmlAttribute]
        public float InnerXMin { get; set; }

        [XmlAttribute]
        public float InnerXMax { get; set; }

        [XmlAttribute]
        public float OuterYMin { get; set; }

        [XmlAttribute]
        public float OuterYMax { get; set; }

        [XmlAttribute]
        public float OuterXMin { get; set; }

        [XmlAttribute]
        public float OuterXMax { get; set; }
    }
}
