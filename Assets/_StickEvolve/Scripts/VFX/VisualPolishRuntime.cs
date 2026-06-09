using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace StickEvolve.VFX
{
    /// <summary>
    /// Runtime-постпроцесс для code-generated сцены. Без ассетов/профилей в проекте:
    /// создаём Volume прямо из кода, чтобы картинка стала ближе к fantasy vertical slice.
    /// </summary>
    public static class VisualPolishRuntime
    {
        private static Volume _volume;

        public static void Apply3D(Camera cam)
        {
            if (cam == null) return;
            cam.allowHDR = true;
            cam.backgroundColor = new Color(0.035f, 0.025f, 0.065f);
            var urpCamera = cam.gameObject.GetComponent<UniversalAdditionalCameraData>();
            if (urpCamera == null) urpCamera = cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
            urpCamera.renderPostProcessing = true;
            urpCamera.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
            EnsureVolume();
        }

        private static void EnsureVolume()
        {
            if (_volume != null) return;

            var go = new GameObject("StickEvolve_GlobalPostProcess");
            Object.DontDestroyOnLoad(go);
            _volume = go.AddComponent<Volume>();
            _volume.isGlobal = true;
            _volume.priority = 25f;

            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = "StickEvolve_Runtime_Polish";
            _volume.profile = profile;

            var bloom = profile.Add<Bloom>(true);
            bloom.intensity.Override(0.75f);
            bloom.threshold.Override(0.72f);
            bloom.scatter.Override(0.62f);

            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0.26f);
            vignette.smoothness.Override(0.55f);
            vignette.color.Override(new Color(0.035f, 0.015f, 0.045f));

            var color = profile.Add<ColorAdjustments>(true);
            color.postExposure.Override(0.10f);
            color.contrast.Override(22f);
            color.saturation.Override(14f);
            color.colorFilter.Override(new Color(1.0f, 0.92f, 1.08f));

            var chroma = profile.Add<ChromaticAberration>(true);
            chroma.intensity.Override(0.045f);
        }
    }
}
