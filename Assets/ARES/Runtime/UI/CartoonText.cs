using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ares.UI
{
    /// <summary>Reusable decorative sprite lettering. Ordinary labels remain the body-text/accessibility fallback.</summary>
    public sealed class CartoonText : VisualElement
    {
        [Serializable] sealed class Glyph { public string character,atlas; public int x,y,width,height,textureWidth,textureHeight; }
        [Serializable] sealed class Catalog { public Glyph[] glyphs; }
        static Dictionary<char,Glyph> glyphs;
        static readonly Dictionary<string,Texture2D> textures=new Dictionary<string,Texture2D>();
        public static int GlyphCount { get { Load();return glyphs.Count; } }
        static void Load()
        {
            if(glyphs!=null)return;
            glyphs=new Dictionary<char,Glyph>();
            var source=Resources.Load<TextAsset>("ARES/UI/CartoonAlphabet/glyphs");
            if(source==null)return;
            foreach(var glyph in JsonUtility.FromJson<Catalog>(source.text.TrimStart('\uFEFF')).glyphs)glyphs[glyph.character[0]]=glyph;
        }
        public CartoonText(string text,float size=32)
        {
            Load();tooltip=text;name="cartoon-text";pickingMode=PickingMode.Ignore;
            style.flexDirection=FlexDirection.Row;style.alignItems=Align.FlexEnd;style.flexShrink=0;style.height=size*1.3f;
            foreach(char character in text.ToUpperInvariant())
            {
                if(character==' ') { var space=new VisualElement();space.style.width=size*.35f;Add(space);continue; }
                if(!glyphs.TryGetValue(character,out var glyph)) { var fallback=new Label(character.ToString());fallback.style.fontSize=size;Add(fallback);continue; }
                if(!textures.TryGetValue(glyph.atlas,out var texture)) { texture=Resources.Load<Texture2D>("ARES/UI/CartoonAlphabet/"+glyph.atlas);textures[glyph.atlas]=texture; }
                float height=size*(character>127?1.25f:1);
                var image=new Image {image=texture,uv=new Rect((float)glyph.x/glyph.textureWidth,(float)glyph.y/glyph.textureHeight,(float)glyph.width/glyph.textureWidth,(float)glyph.height/glyph.textureHeight),scaleMode=ScaleMode.StretchToFill,pickingMode=PickingMode.Ignore};
                image.style.height=height;image.style.width=height*glyph.width/glyph.height;image.style.marginRight=size*.055f;Add(image);
            }
        }
    }
}
