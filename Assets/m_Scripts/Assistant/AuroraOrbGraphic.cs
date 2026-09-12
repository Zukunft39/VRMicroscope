using UnityEngine;
using UnityEngine.UI;

namespace VRMicroscope.Assistant
{
    // Translucent sphere and flowing aurora, using monochrome contrast rather than a logo silhouette.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class AuroraOrbGraphic : MaskableGraphic
    {
        public bool reducedMotion;
        public bool speaking;
        private float phase;
        private bool wasSpeaking;

        private void Update()
        {
            float next = reducedMotion ? 0 : Time.unscaledTime;
            if (next != phase || wasSpeaking != speaking)
            {
                phase = next;
                wasSpeaking = speaking;
                SetVerticesDirty();
            }
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Vector2 center = rectTransform.rect.center;
            float size = Mathf.Min(rectTransform.rect.width, rectTransform.rect.height) * .5f;
            if (size <= 0) return;
            float breath = speaking && !reducedMotion ? 1 + .025f*Mathf.Sin(phase*2) : 1;
            float shell = size*.51f*breath;
            // A very light neutral shell: no opaque fill, directional shading or specular hotspot.
            Disc(mesh,center,shell,new Color(1,1,1,.20f),new Color(1,1,1,.10f));
            for (int i=0;i<128;i++)
            {
                float a=i*Mathf.PI*2/128, b=(i+1)*Mathf.PI*2/128;
                Vector2 u=center+Direction(a)*shell, v=center+Direction(b)*shell;
                FeatherStrip(mesh,center,u,v,size*.045f,new Color(1,1,1,.15f));
            }
            // Restore the original three continuous, undulating aurora ribbons.
            for(int band=0;band<3;band++)
            {
                float radius=size*(.64f+band*.083f);
                for(int i=0;i<128;i++)
                {
                    float a=i*Mathf.PI*2/128, b=(i+1)*Mathf.PI*2/128;
                    float pulse=.5f+.5f*Mathf.Sin(a*3+phase*.65f+band);
                    Vector2 u=Ribbon(center,radius,a,band), v=Ribbon(center,radius,b,band);
                    float width=size*(.025f+pulse*.045f);
                    FeatherStrip(mesh,center,u,v,width,new Color(1,1,1,.12f+.20f*pulse));
                    // Narrow dark backing and a white filament retain contrast on lab backgrounds.
                    Stroke(mesh,center,u,v,size*.023f,new Color(0,0,0,.48f));
                    Stroke(mesh,center,u,v,size*.009f,new Color(1,1,1,.52f+.25f*pulse));
                }
            }
            Ring(mesh,center,shell+size*.025f,size*.040f,new Color(0,0,0,.82f));
            Ring(mesh,center,shell,size*.020f,new Color(1,1,1,.94f));
            for(int i=0;i<7;i++)
            {
                float a=i*2.39996f+phase*(.15f+i*.012f);
                Vector2 pos=center+Direction(a)*size*(.72f+(i%3)*.09f);
                Disc(mesh,pos,size*.022f,new Color(0,0,0,.65f),Color.clear);
                Disc(mesh,pos,size*.012f,new Color(1,1,1,.85f),Color.clear);
            }
        }

        private static Vector2 Direction(float angle) => new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));
        private Vector2 Ribbon(Vector2 center,float radius,float angle,int band)
        {
            float wave=Mathf.Sin(angle*3+phase*.8f+band*.7f)*radius*.08f;
            return center+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle)*(.77f+.10f*Mathf.Sin(phase*.25f+band)))*(radius+wave);
        }
        private static void Ring(VertexHelper mesh,Vector2 center,float radius,float width,Color color)
        {
            for(int i=0;i<128;i++)
            {
                Vector2 u=center+Direction(i*Mathf.PI*2/128)*radius;
                Vector2 v=center+Direction((i+1)*Mathf.PI*2/128)*radius;
                Stroke(mesh,center,u,v,width,color);
            }
        }
        private static void Stroke(VertexHelper mesh,Vector2 center,Vector2 u,Vector2 v,float width,Color color)
        {
            Vector2 du=(u-center).normalized*width*.5f,dv=(v-center).normalized*width*.5f;
            Quad(mesh,u-du,v-dv,v+dv,u+du,color,color);
        }
        private static void FeatherStrip(VertexHelper mesh,Vector2 center,Vector2 u,Vector2 v,float width,Color color)
        {
            Vector2 du=(u-center).normalized*width,dv=(v-center).normalized*width;
            Color clear=new Color(color.r,color.g,color.b,0);
            Quad(mesh,u-du,v-dv,v,u,clear,color);
            Quad(mesh,u,v,v+dv,u+du,color,clear);
        }
        private static void Disc(VertexHelper mesh,Vector2 center,float radius,Color inner,Color outer)
        {
            int start=mesh.currentVertCount;
            mesh.AddVert(center,inner,Vector2.zero);
            for(int i=0;i<=64;i++)
            {
                mesh.AddVert(center+Direction(i*Mathf.PI*2/64)*radius,outer,Vector2.zero);
                if(i>0) mesh.AddTriangle(start,start+i,start+i+1);
            }
        }
        private static void Quad(VertexHelper mesh,Vector2 a,Vector2 b,Vector2 c,Vector2 d,Color inner,Color outer)
        {
            int index=mesh.currentVertCount;
            mesh.AddVert(a,inner,Vector2.zero); mesh.AddVert(b,inner,Vector2.zero);
            mesh.AddVert(c,outer,Vector2.zero); mesh.AddVert(d,outer,Vector2.zero);
            mesh.AddTriangle(index,index+1,index+2); mesh.AddTriangle(index,index+2,index+3);
        }
    }
}
