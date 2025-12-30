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
    Rigidbody2D rb2d;

    int currentDestination = 0;

    Coroutine actCoroutine;
    void Start()
    {
        rb2d = GetComponent<Rigidbody2D>();
        sprite = GetComponentInChildren<SpriteRenderer>();
        anim = GetComponentInChildren<Animator>();

        actCoroutine ??= StartCoroutine(Act(true));
    }

    private void OnEnable()
    {
        Start();
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        actCoroutine = null;
        anim.ResetTrigger("touch");
        anim.SetBool("sit", false);
        anim.SetBool("move", false);
        rb2d.velocity = Vector2.zero;
        isTouching = false;
    }

    private void Update()
    {
        anim.SetBool("move", rb2d.velocity.magnitude > 0);

        if (rb2d.velocity.x > 0)
            sprite.flipX = false;
        else if (rb2d.velocity.x < 0)
            sprite.flipX = true;
    }

    float SitChance = 15;
    IEnumerator Act(bool wait = false)
    {
        if (wait) yield return new WaitForSeconds(initWaitTime);
        while (true)
        {
            yield return StartCoroutine(Move());
            if (Random.Range(0, 100) <= SitChance)
            {
                yield return StartCoroutine(Relax());
                SitChance = 0;
            }
            else
            {
                yield return new WaitForSeconds(Random.Range(0, 20f));
                SitChance += 35;
            }
        }
    }

    void OnCheckpointReach()
    {
        currentDestination = (currentDestination + 1) % Checkpoints.Count;
    }

    IEnumerator Move()
    {
        float breakChance = -10;
        var destination = Checkpoints[currentDestination];
        Vector2 direction = (destination.position - transform.position).normalized;
        rb2d.velocity = direction * moveSpd;

        while (Random.Range(0, 100) > breakChance)
        {
            if (Vector2.Distance(transform.position, destination.position) <= 25f)
            {
                OnCheckpointReach();
                break;
            }

            yield return new WaitForSeconds(1f);
            breakChance += 2.5f;
        }

        rb2d.velocity = Vector2.zero;
    }

    IEnumerator Relax()
    {
        anim.SetBool("sit", true);

        float breakChance = -20;
        while (Random.Range(0, 100) > breakChance)
        {
            rb2d.velocity = Vector2.zero;
            yield return new WaitForSeconds(1f);
            breakChance += 2.5f;
        }

        anim.SetBool("sit", false);
    }

    public void OnTouch()
    {
        if (isTouching) return;
        StopAllCoroutines();
        anim.SetBool("sit", false);
        rb2d.velocity = Vector2.zero;
        StartCoroutine(Touch());
    }

    bool isTouching = false;
    IEnumerator Touch()
    {
        isTouching = true;
        rb2d.velocity = Vector2.zero;
        anim.SetTrigger("touch");
        yield return new WaitForSeconds(3.5f);
        StartCoroutine(Act());
        isTouching = false;
    }
}
