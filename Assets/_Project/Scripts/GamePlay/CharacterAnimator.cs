using UnityEngine;
using UnityEngine.AI;
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

        private NavMeshAgent           agent;
        private Animator               animator;
        private PlayableGraph          graph;
        private AnimationMixerPlayable mixer;
        private AnimationClipPlayable  idlePlayable;
        private AnimationClipPlayable  walkPlayable;
        private bool                   isWalking;
        private float                  blend;     // 0 = idle, 1 = walk

        private void Awake()
        {
            animator = GetComponent<Animator>();
            agent    = GetComponentInParent<NavMeshAgent>();
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

            // Агент мог быть добавлен ПОСЛЕ нашего Awake — ищем лениво.
            if (agent == null) agent = GetComponentInParent<NavMeshAgent>();

            float speed = agent != null ? agent.velocity.magnitude : 0f;
            bool walkingNow = speed > velocityThreshold;
            float target = walkingNow ? 1f : 0f;
            blend = Mathf.MoveTowards(blend, target, Time.deltaTime / Mathf.Max(0.01f, blendTime));

            mixer.SetInputWeight(0, 1f - blend);
            mixer.SetInputWeight(1, blend);
            isWalking = walkingNow;
        }

        private void OnDestroy()
        {
            if (graph.IsValid()) graph.Destroy();
        }
    }
}
