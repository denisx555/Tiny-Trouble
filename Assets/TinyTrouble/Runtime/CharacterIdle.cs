using UnityEngine;

namespace TinyTrouble
{
    public sealed class CharacterIdle : MonoBehaviour
    {
        [SerializeField] float tiltDegrees = 1.3f;
        [SerializeField] float speed = 1.7f;
        Vector3 basePosition;
        Quaternion baseRotation;
        void Awake() { basePosition = transform.localPosition; baseRotation = transform.localRotation; }
        void Update()
        {
            var phase = Time.unscaledTime * speed;
            transform.localRotation = baseRotation * Quaternion.Euler(0, Mathf.Sin(phase * .6f) * 2f, Mathf.Sin(phase) * tiltDegrees);
            transform.localPosition = basePosition + Vector3.up * (Mathf.Sin(phase * 2f) * .012f);
        }
    }
}
