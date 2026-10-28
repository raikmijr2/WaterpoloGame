using UnityEngine;
using Waterpulse.Config;
using Waterpulse.Core;

namespace Waterpulse.Gameplay
{
    // Cámara inclinada que sigue la pelota con suavizado y algo de adelanto hacia la portería que ataca
    // el equipo con la pelota. Encuadra un trozo del campo (no todo) y nunca se sale de la piscina
    [RequireComponent(typeof(Camera))]
    public class MatchCamera : MonoBehaviour
    {
        [SerializeField] GameConfigSO config;
        [SerializeField] PoolBounds pool;
        [SerializeField] BallController ball;

        Camera cam;
        Vector3 focus;
        Vector3 focusVelocity;
        bool snapped;
        GameObject controlled;
        float shakeTimer;

        void Awake() => cam = GetComponent<Camera>();

        void OnEnable()
        {
            GameEvents.ControlledPlayerChanged += OnControlledChanged;
            GameEvents.RecoverFailed += OnRecoverFailed;
        }

        void OnDisable()
        {
            GameEvents.ControlledPlayerChanged -= OnControlledChanged;
            GameEvents.RecoverFailed -= OnRecoverFailed;
        }

        void OnControlledChanged(GameObject player) => controlled = player;

        // Temblor pequeño cuando el humano falla un robo con J (no con los de la IA)
        void OnRecoverFailed(GameObject player)
        {
            if (player != null && player == controlled) shakeTimer = config.cameraShakeDuration;
        }

        void LateUpdate()
        {
            if (!Ready()) return;
            Vector3 target = Target();
            if (!snapped)
            {
                focus = target;
                snapped = true;
            }
            focus = Vector3.SmoothDamp(focus, target, ref focusVelocity, config.cameraSmoothTime);
            Place(focus + ShakeOffset());
        }

        // Desplazamiento aleatorio que se va apagando durante el temblor
        Vector3 ShakeOffset()
        {
            if (shakeTimer <= 0f) return Vector3.zero;
            shakeTimer -= Time.deltaTime;
            float fade = config.cameraShakeDuration > 0f ? Mathf.Clamp01(shakeTimer / config.cameraShakeDuration) : 0f;
            Vector2 r = Random.insideUnitCircle * (config.cameraShakeAmplitude * fade);
            return new Vector3(r.x, 0f, r.y);
        }

        // Coloca la cámara directamente sobre su objetivo (la usa el generador de escena)
        public void SnapToTarget()
        {
            if (!Ready()) return;
            focus = Target();
            focusVelocity = Vector3.zero;
            snapped = true;
            Place(focus);
        }

        bool Ready()
        {
            if (config == null || pool == null || ball == null) return false;
            if (cam == null) cam = GetComponent<Camera>();
            return true;
        }

        float Distance() => CameraFraming.DistanceForViewLength(config.cameraViewLength, config.cameraFieldOfView, cam.aspect);

        // La pelota, adelantada hacia donde ataca quien la tiene, sin enseñar más allá del campo
        Vector3 Target()
        {
            float lead = ball.HasPossession(TeamSide.Home) ? TeamSide.Home.AttackSign()
                       : ball.HasPossession(TeamSide.Away) ? TeamSide.Away.AttackSign() : 0f;
            // La posición dibujada (interpolada) de la pelota, no la física: esa solo cambia en cada paso de física
            // (50 por segundo) y la cámara iría a saltos haciendo temblar toda la imagen
            Vector3 target = ball.transform.position + Vector3.right * (lead * config.cameraLead);
            target.y = pool.WaterY;

            float distance = Distance();
            float halfViewX = config.cameraViewLength * 0.5f;
            float halfViewZ = CameraFraming.HalfViewDepth(distance, config.cameraFieldOfView, config.cameraPitch);
            return CameraFraming.ClampFocus(target, pool.Center, pool.HalfLength, pool.HalfWidth, config.cameraMargin, halfViewX, halfViewZ);
        }

        void Place(Vector3 point)
        {
            cam.fieldOfView = config.cameraFieldOfView;
            Quaternion rotation = Quaternion.Euler(config.cameraPitch, 0f, 0f);
            transform.SetPositionAndRotation(point - rotation * Vector3.forward * Distance(), rotation);
        }
    }
}
