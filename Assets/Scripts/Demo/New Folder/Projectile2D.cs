using System;
using UnityEngine;

public class Projectile2D : MonoBehaviour
{
    public float speed = 12f;

    [Header("Safety")]
    public float maxLifeSeconds = 2.0f;
    public float hitDistance = 0.05f;

    private Transform _target;
    private Vector3 _targetPos;
    private Action _onHit;
    private bool _active;
    private float _life;

    public void Launch(Transform target, Action onHit)
    {
        _target = target;
        _onHit = onHit;
        _active = true;
        _life = 0f;

        _targetPos = _target ? _target.position : transform.position;
    }

    void Update()
    {
        if (!_active) return;

        _life += Time.deltaTime;

        if (_target != null) _targetPos = _target.position;

        transform.position = Vector3.MoveTowards(transform.position, _targetPos, speed * Time.deltaTime);

        if (Vector3.SqrMagnitude(transform.position - _targetPos) <= hitDistance * hitDistance)
        {
            Hit();
            return;
        }

        if (_life >= maxLifeSeconds)
        {
            Hit();
        }
    }

    private void Hit()
    {
        if (!_active) return;
        _active = false;

        _onHit?.Invoke();
        Destroy(gameObject);
    }
}
