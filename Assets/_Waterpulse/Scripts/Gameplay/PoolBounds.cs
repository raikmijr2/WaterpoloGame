using UnityEngine;
using Waterpulse.Config;

namespace Waterpulse.Gameplay
{
    // Medidas y límites del campo. Largo en X (porterías en ±X), ancho en Z, agua en la Y del objeto
    public class PoolBounds : MonoBehaviour
    {
        [SerializeField] GameConfigSO config;

        public float HalfLength => config.poolLength * 0.5f;
        public float HalfWidth => config.poolWidth * 0.5f;
        public float WaterY => transform.position.y;
        public Vector3 Center => transform.position;

        public bool Contains(Vector3 point, float margin = 0f)
        {
            Vector3 local = point - Center;
            return Mathf.Abs(local.x) <= HalfLength - margin && Mathf.Abs(local.z) <= HalfWidth - margin;
        }

        public Vector3 ClampInside(Vector3 point, float margin = 0f)
        {
            Vector3 c = Center;
            point.x = Mathf.Clamp(point.x, c.x - HalfLength + margin, c.x + HalfLength - margin);
            point.z = Mathf.Clamp(point.z, c.z - HalfWidth + margin, c.z + HalfWidth - margin);
            return point;
        }

        void OnDrawGizmos()
        {
            if (config == null) return;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(Center, new Vector3(config.poolLength, 0.01f, config.poolWidth));
        }
    }
}
