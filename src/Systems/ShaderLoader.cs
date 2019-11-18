using Newtonsoft.Json;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace MistMod
{
    public class ShaderLoader : ModSystem
    {
        public ICoreClientAPI capi;
        OrthoRenderer[] orthoRenderers;

        public static float NightvisionStrength;
        public static float VigneteStrength;
        long id;

        public string[] orthoShaderKeys;

        public override void StartClientSide(ICoreClientAPI api)
        {
            capi = api;

            id = api.Event.RegisterGameTickListener(dt =>
            {
                if (capi.World.Player?.Entity != null)
                {
                    StartShade(capi.World.Player);
                    api.Event.UnregisterGameTickListener(id);
                }
            }, 500);
        }

        public void StartShade(IPlayer player)
        {
            capi.Event.ReloadShader += LoadShaders;
            LoadShaders();
            if (orthoRenderers == null) return;

            for (int i = 0; i < orthoRenderers.Length; i++)
            {
                capi.Event.RegisterRenderer(orthoRenderers[i], EnumRenderStage.Ortho, orthoShaderKeys[i]);
            }
        }

        public bool LoadShaders()
        {
            List<OrthoRenderer> rendererers = new List<OrthoRenderer>();
            orthoShaderKeys = capi.Assets.TryGet("config/orthoshaderlist.json")?.ToObject<string[]>();
            if (orthoShaderKeys == null) return false;

            for (int i = 0; i < orthoShaderKeys.Length; i++)
            {
                IShaderProgram shader = capi.Shader.NewShaderProgram();
                int program = capi.Shader.RegisterFileShaderProgram(orthoShaderKeys[i], shader);
                shader = capi.Render.GetShader(program);
                shader.Compile();

                OrthoRenderer renderer = new OrthoRenderer(capi, shader);

                if (orthoRenderers != null)
                {
                    orthoRenderers[i].prog = shader;
                    capi.Event.ReRegisterRenderer(orthoRenderers[i], EnumRenderStage.Ortho);
                }
                rendererers.Add(renderer);
            }
            orthoRenderers = rendererers.ToArray();

            return true;
        }

    }

    public class OrthoRenderer : IRenderer
    {
        MeshRef quadRef;
        ICoreClientAPI capi;
        public IShaderProgram prog;

        public Matrixf ModelMat = new Matrixf();

        public double RenderOrder => 0;

        public int RenderRange => 1;

        public OrthoRenderer(ICoreClientAPI api, IShaderProgram prog)
        {
            this.prog = prog;
            capi = api;
            MeshData quadMesh = QuadMeshUtil.GetQuad();
            
            quadMesh.Rgba = null;

            quadRef = capi.Render.UploadMesh(quadMesh);
        }


        public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
        {
            if (prog.Disposed) return;
            IShaderProgram curShader = capi.Render.CurrentActiveShader;
            curShader.Stop();

            prog.Use();
            capi.Render.GlToggleBlend(true);
            prog.SetDefaultUniforms(capi);
            prog.BindTexture2D("iColor", capi.Render.FrameBuffers[(int)EnumFrameBuffer.Primary].ColorTextureIds[0], 0);

            capi.Render.RenderMesh(quadRef);
            prog.Stop();
            curShader.Use();
        }

        public void Dispose()
        {
        }
    }

    public static class DefaultUniforms
    {
        public static void SetDefaultUniforms(this IShaderProgram prog, ICoreClientAPI capi)
        {
            prog.Uniform("iVigneteStrength", ShaderLoader.VigneteStrength);
            prog.Uniform("iNightvisionStrength", ShaderLoader.NightvisionStrength);
        }

        public static void ReRegisterRenderer(this IClientEventAPI events, IRenderer renderer, EnumRenderStage stage)
        {
            renderer.Dispose();
            events.UnregisterRenderer(renderer, stage);
            events.RegisterRenderer(renderer, stage);
        }
    }
}