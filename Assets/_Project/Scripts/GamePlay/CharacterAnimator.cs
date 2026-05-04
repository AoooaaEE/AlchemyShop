using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;

namespace Alchemy.Gameplay
{
    /// <summary>
    /// Подцепляет анимации к KayKit-персонажу с использованием Playable API:
    /// - находит AnimationClip'ы в импортированном glTF (Idle, Running_A, ...)
    /// - переключает Idle ↔ Running_A в зависимости от скорости NavMeshAgent.
    /// Работает без AnimatorController — клипы из glb проигрываются напрямую.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class CharacterAnimator : MonoBehaviour
    {
        [SerializeField] private float  velocityThreshold = 0.15f;
        [SerializeField] private float  blendTime = 0.18f;

        private Animator               animator;
        private PlayableGraph          graph;
        private AnimationMixerPlayable mixer;
        private AnimationClipPlayable  idlePlayable;
        private AnimationClipPlayable  walkPlayable;
        private float                  blend;     // 0 = idle, 1 = walk
        private Transform              speedRef;  // по чему мерим скорость, если нет NavMeshAgent
        private Vector3                lastPos;
        private float                  trackedSpeed;
        private float                  debugTimer;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            // Меряем по собственному world-положению: при движении родителя оно тоже меняется.
            // (transform.root указывал бы на статичный корень сцены — там скорость всегда 0.)
            speedRef = transform;
            lastPos  = speedRef.position;
        }

        public void Init(AnimationClip idle, AnimationClip walk)
        {
            if (idle == null && walk == null)
            {
                Debug.LogWarning($"[CharacterAnimator] {name}: оба клипа null, пропускаю.");
                return;
            }
            string idleN = idle != null ? idle.name : "null";
            string walkN = walk != null ? walk.name : "null";
            string animOk = animator != null ? "OK" : "null";
            Debug.Log("[CharacterAnimator] " + name + ": Init idle=" + idleN +
                      ", walk=" + walkN + ", animator=" + animOk);

            graph = PlayableGraph.Create("CharacterAnim_" + name);
            graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

            mixer = AnimationMixerPlayable.Create(graph, 2);

            idlePlayable = AnimationClipPlayable.Create(graph, idle != null ? idle : walk);
            walkPlayable = AnimationClipPlayable.Create(graph, walk != null ? walk : idle);
            graph.Connect(idlePlayable, 0, mixer, 0);
            graph.Connect(walkPlayable, 0, mixer, 1);
            mixer.SetInputWeight(0, 1f);
            mixer.SetInputWeight(1, 0f);

            var output = AnimationPlayableOutput.Create(graph, "Anim", animator);
            output.SetSourcePlayable(mixer);
            graph.Play();
        }

        private void Update()
        {
            if (!graph.IsValid()) return;

            // Скорость всегда меряем по transform-дельте — единый код для NPC и игрока.
            // (У игрока NavMeshAgent.velocity не обновляется, т.к. он двигается через agent.Move().)
            if (speedRef == null) speedRef = transform;
            Vector3 pos = speedRef.position;
            Vector3 delta = pos - lastPos;
            delta.y = 0f;
            float instant = Time.deltaTime > 0.0001f ? delta.magnitude / Time.deltaTime : 0f;
            trackedSpeed = Mathf.Lerp(trackedSpeed, instant, 0.4f);
            float speed = trackedSpeed;
            lastPos = pos;

            bool walkingNow = speed > velocityThreshold;
            float target = walkingNow ? 1f : 0f;
            blend = Mathf.MoveTowards(blend, target, Time.deltaTime / Mathf.Max(0.01f, blendTime));

            mixer.SetInputWeight(0, 1f - blend);
            mixer.SetInputWeight(1, blend);

            debugTimer += Time.deltaTime;
            if (debugTimer > 2f)
            {
                debugTimer = 0f;
                Debug.Log("[CharacterAnimator] " + name + ": speed=" +
                          speed.ToString("F2") + ", blend=" + blend.ToString("F2"));
            }
        }

        private void OnDestroy()
        {
            if (graph.IsValid()) graph.Destroy();
        }
    }
}
