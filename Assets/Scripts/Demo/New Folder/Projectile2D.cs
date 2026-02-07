using System;
using UnityEngine;

public class Projectile2D : MonoBehaviour
{
    public float speed = 12f;

    private Transform _target;
    private Action _onHit;
    private bool _active;

    public void Launch(Transform target, Action onHit)
    {
        _target = target;
        _onHit = onHit;
        _active = true;
    }

    void Update()
    {
        if (!_active || _target == null) return;

        Vector3 t = _target.position;
        transform.position = Vector3.MoveTowards(transform.position, t, speed * Time.deltaTime);

        // chạm gần target thì coi như hit
        if (Vector3.SqrMagnitude(transform.position - t) < 0.05f * 0.05f)
        {
            Hit();
        }
    }

    private void Hit()
    {
        _active = false;
        _onHit?.Invoke();
        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // nếu bạn muốn “đụng collider” mới hit thì dùng cái này thay cho khoảng cách
        // Hit();
    }
}
