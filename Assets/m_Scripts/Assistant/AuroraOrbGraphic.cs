using UnityEngine;
using UnityEngine.UI;

namespace VRMicroscope.Assistant
{
    // Procedural UI geometry: no generated textures, shaders, or per-frame material allocations.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class AuroraOrbGraphic : MaskableGraphic
    {
        public bool reducedMotion;
        public bool speaking;
        private float phase;
        private void Update()
        {
            float previousPhase = phase;
            phase = reducedMotion ? 0 : Time.unscaledTime;
            if (phase != previousPhase) SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Vector2 center = rectTransform.rect.center;
            float size = Mathf.Min(rectTransform.rect.width, rectTransform.rect.height) * .5f;
            // A transparent energy shell, not a shaded, opaque physical sphere.
            Disc(mesh, center, size * .94f, new Color(.32f,.86f,1f,.025f), Color.clear);
            float breath = speaking && !reducedMotion ? 1 + .025f*Mathf.Sin(phase*2) : 1;
            float shell = size * .51f * breath;
            Disc(mesh, center, shell, new Color(.40f,.88f,1f,.015f), new Color(.30f,.76f,1f,.07f));
            for (int i=0; i<128; i++)
            {
                float a=i*Mathf.PI*2/128, b=(i+1)*Mathf.PI*2/128;
                Vector2 u=center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*shell;
                Vector2 v=center+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*shell;
                float shimmer=.5f+.5f*Mathf.Sin(a*2-phase*.4f);
                FeatherStrip(mesh,center,u,v,size*.035f,
                    new Color(.42f,.87f,1f,.12f+.13f*shimmer));
            }
            for (int band = 0; band < 3; band++)
            {
                float radius = size * (.64f + band * .083f);
                for (int i = 0; i < 96; i++)
                {
                    float a = i * Mathf.PI * 2 / 96;
                    float b = (i + 1) * Mathf.PI * 2 / 96;
                    float pulse = .5f + .5f * Mathf.Sin(a * 3 + phase * .65f + band);
                    Color c = Color.Lerp(new Color(.28f,.92f,.89f), new Color(.62f,.56f,1), pulse);
                    c.a = .055f + .16f * pulse;
                    float width = size * (.035f + pulse * .055f);
                    Vector2 u = Ribbon(center, radius, a, band);
                    Vector2 v = Ribbon(center, radius, b, band);
                    FeatherStrip(mesh, center, u, v, width, c);
                }
            }
            for (int i=0; i<7; i++)
            {
                float a = i*2.39996f + phase*(.15f+i*.012f);
                Vector2 pos = center + new Vector2(Mathf.Cos(a),Mathf.Sin(a)) * size*(.72f+(i%3)*.09f);
                Disc(mesh,pos,size*.024f,new Color(.60f,.95f,1,.30f),Color.clear);
            }
        }
        private static void FeatherStrip(VertexHelper m,Vector2 center,Vector2 u,Vector2 v,float width,Color color)
        {
            Vector2 du=(u-center).normalized*width, dv=(v-center).normalized*width;
            Color transparent=new Color(color.r,color.g,color.b,0);
            Quad(m,u-du,v-dv,v,u,transparent,color);
            Quad(m,u,v,v+dv,u+du,color,transparent);
        }
        private Vector2 Ribbon(Vector2 c, float r, float a, int band)
        {
            float wave = Mathf.Sin(a*3 + phase*.8f + band*.7f)*r*.08f;
            return c + new Vector2(Mathf.Cos(a),Mathf.Sin(a)*(.77f+.10f*Mathf.Sin(phase*.25f+band))) * (r+wave);
        }
        private static void Disc(VertexHelper m, Vector2 c, float radius, Color inner, Color outer)
        {
            int start=m.currentVertCount;
            m.AddVert(c,inner,Vector2.zero);
            for(int i=0;i<=48;i++)
            {
                float a=i*Mathf.PI*2/48;
                m.AddVert(c+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,outer,Vector2.zero);
                if(i>0) m.AddTriangle(start,start+i,start+i+1);
            }
        }
        private static void Quad(VertexHelper m,Vector2 a,Vector2 b,Vector2 c,Vector2 d,Color inner,Color outer)
        {
            int i=m.currentVertCount;
            m.AddVert(a,inner,Vector2.zero); m.AddVert(b,inner,Vector2.zero);
            m.AddVert(c,outer,Vector2.zero); m.AddVert(d,outer,Vector2.zero);
            m.AddTriangle(i,i+1,i+2); m.AddTriangle(i,i+2,i+3);
        }
    }
}
