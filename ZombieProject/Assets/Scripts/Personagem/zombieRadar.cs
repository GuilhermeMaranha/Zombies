using UnityEngine;

public class zombieRadar : MonoBehaviour
{
    [Header("Detecção")]
    [SerializeField] float detectionRadius = 8f;
    [SerializeField] string playerTag = "Player";

    [Header("Movimento")]
    [SerializeField] float walkSpeed = 3.2f;
    [SerializeField] float runSpeed = 5.4f;
    [SerializeField] float rotationSpeed = 7f;
    [SerializeField] float attackRange = 1.3f;

    [Header("Troca para Corrida (distâncias)")]
    [SerializeField] float runEnterDistance = 2.8f;
    [SerializeField] float runExitDistance = 3.4f;

    [Header("Animação")]
    [SerializeField] Animator zombieAnim; // precisa ter bool "Moving", bool "isRun", trigger "Attack"

    [Header("Áudio")]
    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioClip detectClip;
    [SerializeField] AudioClip runClip;
    [SerializeField] AudioClip[] attacks;
    [SerializeField] AudioClip deathClip;
    [SerializeField] AudioClip[] groans;
    [SerializeField] Vector2 groanExtraGapRange = new Vector2(0.6f, 2.0f);

    Transform target;
    Rigidbody rb;
    bool chasing;
    bool dead;
    bool isRunning;

    // flags de áudio
    bool playedDetectThisChase;
    bool playedRunThisChase;

    enum SfxState { None, Groan, Detect, Run, Attack, Death }
    SfxState currentSfx = SfxState.None;
    float sfxEndTime;
    float nextGroanEarliestTime;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (runExitDistance < runEnterDistance) runExitDistance = runEnterDistance + 0.4f;
    }

    void Update()
    {
        if (dead) { SetMoving(false); SetRun(false); return; }

        DetectPlayerSphere();

        if (!target)
        {
            SetMoving(false);
            SetRun(false);
            return;
        }

        Vector3 to = target.position - transform.position;
        Vector3 flat = new Vector3(to.x, 0f, to.z);

        if (flat.sqrMagnitude > 0.001f)
        {
            Quaternion look = Quaternion.LookRotation(flat.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, look, rotationSpeed * 100f * Time.deltaTime
            );
        }

        float dist = flat.magnitude;

        // decide andar/correr
        if (dist <= runEnterDistance)
        {
            if (!isRunning)
            {
                SetRun(true);
                TryPlayRun();
            }
        }
        else if (dist >= runExitDistance)
        {
            if (isRunning) SetRun(false);
        }

        if (dist > attackRange)
        {
            float speed = isRunning ? runSpeed : walkSpeed;
            Vector3 step = flat.normalized * speed * Time.deltaTime;
            if (rb) rb.MovePosition(transform.position + step);
            else transform.position += step;

            SetMoving(true);
        }
        else
        {
            SetMoving(false);
            SetRun(false);
            TriggerAttack();
        }
    }

    void DetectPlayerSphere()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius);
        Transform nearest = null;
        float bestSqr = float.MaxValue;

        foreach (var h in hits)
        {
            if (!h.CompareTag(playerTag)) continue;
            float sqr = (h.transform.position - transform.position).sqrMagnitude;
            if (sqr < bestSqr) { bestSqr = sqr; nearest = h.transform; }
        }

        if (nearest)
        {
            if (!chasing)
            {
                chasing = true;
                target = nearest;
                playedDetectThisChase = false;
                playedRunThisChase = false;
                TryPlayDetect();
            }
            else target = nearest;
        }
        else
        {
            chasing = false;
            target = null;
            playedDetectThisChase = false;
            playedRunThisChase = false;
        }
    }

    void SetMoving(bool moving)
    {
        if (zombieAnim) zombieAnim.SetBool("Moving", moving);
        if (moving && Time.time >= sfxEndTime && Time.time >= nextGroanEarliestTime)
            TryPlayGroan();
    }

    void SetRun(bool run)
    {
        isRunning = run;
        if (zombieAnim) zombieAnim.SetBool("isRun", run);
    }

    void TriggerAttack()
    {
        if (dead) return;
        if (zombieAnim) zombieAnim.SetTrigger("Attack");
        TryPlayAttack();
    }

    // ===== ÁUDIO =====
    void TryPlayDetect()
    {
        if (playedDetectThisChase) return;
        if (audioSource && detectClip) PlayExclusive(detectClip, SfxState.Detect, true);
        playedDetectThisChase = true;
    }

    void TryPlayRun()
    {
        if (playedRunThisChase) return;
        if (audioSource && runClip) PlayExclusive(runClip, SfxState.Run, true);
        playedRunThisChase = true;
    }

    void TryPlayAttack()
    {
        if (!audioSource || attacks.Length == 0) return;
        if (currentSfx == SfxState.Attack && Time.time < sfxEndTime) return;

        var clip = attacks[Random.Range(0, attacks.Length)];
        PlayExclusive(clip, SfxState.Attack, true);
    }

    void TryPlayGroan()
    {
        if (!audioSource || groans.Length == 0) return;
        if (Time.time < sfxEndTime) return;

        var clip = groans[Random.Range(0, groans.Length)];
        PlayExclusive(clip, SfxState.Groan, false);
        nextGroanEarliestTime = sfxEndTime + Random.Range(groanExtraGapRange.x, groanExtraGapRange.y);
    }

    void PlayExclusive(AudioClip clip, SfxState newState, bool interrupt)
    {
        if (!clip || !audioSource) return;
        if (interrupt) audioSource.Stop();

        float prev = audioSource.pitch;
        audioSource.pitch = Random.Range(0.96f, 1.04f);
        audioSource.PlayOneShot(clip);
        audioSource.pitch = prev;

        currentSfx = newState;
        sfxEndTime = Time.time + clip.length;
    }

    // ===== Morte =====
    public void OnDeath()
    {
        if (dead) return;
        dead = true;
        SetMoving(false);
        SetRun(false);
        if (audioSource && deathClip)
        {
            audioSource.Stop();
            PlayExclusive(deathClip, SfxState.Death, true);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
