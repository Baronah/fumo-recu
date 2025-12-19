using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MintScript : MonoBehaviour
{
    [SerializeField] List<Transform> Checkpoints;
    [SerializeField] float moveSpd = 10f;
    [SerializeField] float initWaitTime = 15f;

    Animator anim;
    SpriteRenderer sprite;

    void Start()
    {
        sprite = GetComponentInChildren<SpriteRenderer>();
        anim = GetComponentInChildren<Animator>();
        StartCoroutine(Move());
    }

    IEnumerator Move()
    {
        yield return new WaitForSeconds(initWaitTime + Random.Range(0, 20f));

        while (true)
        {
            foreach (Transform cp in Checkpoints)
            {
                yield return StartCoroutine(MoveTowardsCP(cp));
                yield return new WaitForSeconds(Random.Range(3f, 15f));
            }
        }
    }

    IEnumerator MoveTowardsCP(Transform target)
    {
        anim.SetBool("move", true);
        sprite.flipX = target.position.x < transform.position.x;
        float duration = Vector3.Distance(transform.position, target.position) / moveSpd;
        float countUp = 0;
        Vector3 initPos = transform.position;
        while (countUp < duration)
        {
            float LerpValue = countUp / duration;
            transform.position = Vector3.Lerp(initPos, target.position, LerpValue);
            countUp += Time.deltaTime;
            yield return null;
        }
        transform.position = target.position;
        anim.SetBool("move", false);
    }
}
