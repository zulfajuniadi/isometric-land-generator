using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TilemapGenerator.Behaviours;

[ExecuteInEditMode]
public class Animal : MonoBehaviour
{
    public Animator Animator;
    public SpriteRenderer SpriteRenderer;
    public Vector2 Target;
    public float MoveSpeed = 1f;
    public float waitTime = 0;
    public Queue<Vector2> waypoints = new Queue<Vector2>();
    public bool Move = true;

    private LandGenerator Generator;
    float halfMap;
    float quartMap;
    private Quaternion rotation = Quaternion.Euler(0, 0, -45);

    private void Start()
    {
        Generator = GameObject.FindObjectOfType<LandGenerator>();
        halfMap = Generator.ChunkSize / 2f;
        quartMap = Generator.ChunkSize / 4f;
    }

    void Update()
    {
        Vector3 pos = transform.position;
        pos.z = pos.y * 0.1f;
        transform.position = pos;
        if (waitTime <= 0)
        {
            Vector2 heading = Target - (Vector2) transform.position;
            float distance = heading.magnitude;
            if (Target == Vector2.zero)
            {
                GetDestination();
                Animator.SetBool("IsMoving", true);
            }
            else if (distance < 0.1f)
            {
                if (waypoints.Count == 0)
                {
                    Target = Vector2.zero;
                    waitTime = Random.Range(1f, 2f);
                    Animator.SetBool("IsMoving", false);
                }
                else
                {
                    Target = waypoints.Dequeue();
                }
            }
            else
            {
                Vector2 mapHeading = (rotation * heading).normalized;
                SpriteRenderer.flipX = heading.x < 0;
                Animator.SetFloat("Facing", -Mathf.Sign(heading.y));
                Vector3 position = transform.position + (Vector3) (heading.normalized * MoveSpeed * Time.deltaTime);
                if (Move)
                    transform.position = position;
            }
        }
        else
        {
            waitTime -= Time.deltaTime;
        }
    }

    public void GetDestination()
    {
        Vector2 target = new Vector2(
            Random.Range(-2f, 2f),
            Random.Range(-2f, 2f)
        );
        Vector2 mid = new Vector2(
            transform.position.x,
            target.y
        );
        waypoints.Enqueue(target);
        Target = mid;
    }
}
