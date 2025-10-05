#region Using Statements
using UnityEngine;
using System.Collections;
#endregion

[System.Serializable]
public class WeaponSlot
{
    [Header("Identidade")]
    public string id = "bat";

    [Header("Prefab já parented no rig (mão)")]
    public GameObject weaponGO;      // desative no início
    public WeaponHitbox hitbox;      // se null, pega do filho no Awake

    [Header("Status")]
    public bool unlocked = false;    // vira true ao coletar no chão
}

public class ControlePersonagem : MonoBehaviour
{
    #region Fields
    [Header("Movimento")]
    [SerializeField] float walkSpeed = 3.5f;
    [SerializeField] float runSpeed = 6.0f;
    [SerializeField] float rotateSpeed = 220f;

    [Header("Detecção de Ameaça (Zumbi)")]
    [SerializeField] string zombieTag = "Zombie";
    [SerializeField] float threatDetectRadius = 12f;   // raio para procurar zumbis
    [SerializeField] float runEnterDistance = 4.0f;    // começa a correr se um zumbi estiver <= isso
    [SerializeField] float runExitDistance = 5.0f;     // volta a andar se todos > isso

    [Header("Animação")]
    [SerializeField] Animator anim; // usa "Parado"; opcional: "isRun"

    [Header("Slots de Armas em Mão (já parented)")]
    [Tooltip("Liste aqui TODAS as armas que podem ficar na mão (desative todas no início).")]
    [SerializeField] WeaponSlot[] weapons;
    [SerializeField] Transform handBone; // opcional/informativo

    [Header("Pickup")]
    [SerializeField] string pickupTag = "Weapon"; // tag do item no chão

    [Header("Combate")]
    [SerializeField] float attackTotalDuration = 0.60f; // lock total do ataque
    [SerializeField] float hitboxWindup = 0.10f;        // atraso antes de ligar hitbox
    [SerializeField] float hitboxActiveTime = 0.35f;    // tempo que o hitbox fica ligado

    [Header("Áudio")]
    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioClip[] footstepClips;  // passos
    [SerializeField] float stepInterval = 0.4f;  // intervalo base andando
    [SerializeField] AudioClip pickupClip;       // som ao coletar
    [SerializeField] AudioClip[] attackClips;    // som ataque

    Rigidbody rb;
    bool attacking;
    bool canAttack;           // true se tiver arma equipada e liberada
    int currentWeaponIndex = -1;

    // Movimento dinâmico
    bool isRunning;
    float currentSpeed;

    // Controle de passos
    float nextStepTime;
    WeaponHitbox currentHitbox;
    #endregion

    #region Unity Methods
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb) rb.isKinematic = true;

        // corrige histerese mínima
        if (runExitDistance < runEnterDistance) runExitDistance = runEnterDistance + 0.5f;

        // Garante que cada slot tenha referência de hitbox e desativa tudo
        for (int i = 0; i < weapons.Length; i++)
        {
            var slot = weapons[i];
            if (slot.weaponGO)
            {
                if (slot.hitbox == null)
                    slot.hitbox = slot.weaponGO.GetComponentInChildren<WeaponHitbox>(true);

                if (slot.hitbox) slot.hitbox.EnableHitbox(false);
                slot.weaponGO.SetActive(false);
            }
        }

        currentWeaponIndex = -1;
        currentHitbox = null;
        canAttack = false;

        isRunning = false;
        currentSpeed = walkSpeed;
    }

    void Update()
    {
        // 1) Detecta ameaça e decide se corre/anda
        DetectZombieThreatAndSetRun();

        float v = 0f;
        float h = 0f;

        if (!attacking)
        {
            if (Input.GetKey(KeyCode.W)) v = 1f;
            else if (Input.GetKey(KeyCode.S)) v = -1f;

            if (Input.GetKey(KeyCode.A)) h = -1f;
            else if (Input.GetKey(KeyCode.D)) h = 1f;

            if (Mathf.Abs(v) > 0.01f)
                transform.position += transform.forward * (v * currentSpeed * Time.deltaTime);

            if (Mathf.Abs(h) > 0.01f)
                transform.Rotate(0f, h * rotateSpeed * Time.deltaTime, 0f);
        }

        // Animator: "Parado" e (opcional) "isRun"
        bool parado = (Mathf.Abs(v) < 0.01f && Mathf.Abs(h) < 0.01f);
        if (anim)
        {
            anim.SetBool("Parado", parado);
            // Se seu controller tiver essa flag, ela troca a animação de locomoção
            anim.SetBool("isRun", isRunning);
        }

        // Passos (ritmo escala com velocidade atual)
        if (!parado && !attacking && Mathf.Abs(v) > 0.01f)
            TryPlayFootstep();

        // Atacar
        if (Input.GetKeyDown(KeyCode.Space)) Attack();

        // Trocar de arma (F)
        if (Input.GetKeyDown(KeyCode.F)) NextWeapon();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(pickupTag)) return;

        WeaponPickup pickup = other.GetComponent<WeaponPickup>();
        if (!pickup) pickup = other.GetComponentInParent<WeaponPickup>();
        if (!pickup) return;

        UnlockWeapon(pickup.weaponIndex, pickup.autoEquipOnPickup);

        PlayOneShot(pickupClip, Random.Range(0.95f, 1.05f));

        Destroy(other.gameObject);
    }
    #endregion

    #region Threat / Run Logic
    void DetectZombieThreatAndSetRun()
    {
        // Procura zumbis próximos
        Collider[] hits = Physics.OverlapSphere(transform.position, threatDetectRadius);
        float nearest = float.MaxValue;

        for (int i = 0; i < hits.Length; i++)
        {
            if (!hits[i].CompareTag(zombieTag)) continue;
            float d = Vector3.Distance(transform.position, hits[i].transform.position);
            if (d < nearest) nearest = d;
        }

        bool shouldRun = false;

        if (nearest < float.MaxValue)
        {
            // Histerese: entra em corrida quando <= enter; só sai quando >= exit
            if (!isRunning && nearest <= runEnterDistance) shouldRun = true;
            else if (isRunning && nearest < runExitDistance) shouldRun = true;
        }

        SetRun(shouldRun);
    }

    void SetRun(bool run)
    {
        isRunning = run;
        currentSpeed = isRunning ? runSpeed : walkSpeed;
    }
    #endregion

    #region Weapons
    void EquipWeapon(int index)
    {
        if (index < 0 || index >= weapons.Length) return;
        if (!weapons[index].unlocked) return;

        if (currentWeaponIndex >= 0 && currentWeaponIndex < weapons.Length)
        {
            var cur = weapons[currentWeaponIndex];
            if (cur.hitbox) cur.hitbox.EnableHitbox(false);
            if (cur.weaponGO) cur.weaponGO.SetActive(false);
        }

        var slot = weapons[index];
        if (slot.weaponGO) slot.weaponGO.SetActive(true);
        currentHitbox = slot.hitbox;
        if (currentHitbox) currentHitbox.EnableHitbox(false);

        currentWeaponIndex = index;
        canAttack = true;
    }

    void NextWeapon()
    {
        if (CountUnlockedWeapons() <= 1) return;

        int start = currentWeaponIndex;
        for (int i = 1; i <= weapons.Length; i++)
        {
            int next = (start + i) % weapons.Length;
            if (weapons[next].unlocked)
            {
                EquipWeapon(next);
                break;
            }
        }
    }

    int CountUnlockedWeapons()
    {
        int c = 0;
        for (int i = 0; i < weapons.Length; i++)
            if (weapons[i].unlocked) c++;
        return c;
    }

    public void UnlockWeapon(int index, bool autoEquip)
    {
        if (index < 0 || index >= weapons.Length)
        {
            Debug.LogWarning($"[ControlePersonagem] weaponIndex inválido ({index}).");
            return;
        }

        weapons[index].unlocked = true;

        if (weapons[index].weaponGO)
        {
            if (weapons[index].hitbox == null)
                weapons[index].hitbox = weapons[index].weaponGO.GetComponentInChildren<WeaponHitbox>(true);

            if (weapons[index].hitbox) weapons[index].hitbox.EnableHitbox(false);
            weapons[index].weaponGO.SetActive(false);
        }

        if (currentWeaponIndex == -1 || autoEquip)
            EquipWeapon(index);
    }
    #endregion

    #region Combat
    void Attack()
    {
        if (attacking) return;
        if (!canAttack || currentWeaponIndex < 0) return;

        StartCoroutine(AttackRoutine());
    }

    IEnumerator AttackRoutine()
    {
        attacking = true;

        if (anim) anim.SetTrigger("Attack");

        PlayRandom(attackClips, Random.Range(0.95f, 1.05f));

        if (hitboxWindup > 0f)
            yield return new WaitForSeconds(hitboxWindup);

        if (currentHitbox)
            currentHitbox.EnableFor(hitboxActiveTime);

        float remain = Mathf.Max(0f, attackTotalDuration - hitboxWindup);
        if (remain > 0f)
            yield return new WaitForSeconds(remain);

        attacking = false;
    }
    #endregion

    #region Audio (footsteps)
    void TryPlayFootstep()
    {
        if (Time.time < nextStepTime) return;

        // Cadência escala com a velocidade atual (corre = passos mais frequentes)
        float speedScale = Mathf.Clamp(currentSpeed / Mathf.Max(0.01f, walkSpeed), 1f, 2.2f);
        float interval = stepInterval / speedScale;

        PlayRandom(footstepClips, Random.Range(0.95f, 1.05f));
        nextStepTime = Time.time + interval;
    }

    void PlayRandom(AudioClip[] clips, float pitch = 1f)
    {
        if (!audioSource || clips == null || clips.Length == 0) return;
        int i = Random.Range(0, clips.Length);
        PlayOneShot(clips[i], pitch);
    }

    void PlayOneShot(AudioClip clip, float pitch = 1f)
    {
        if (!audioSource || clip == null) return;
        float prevPitch = audioSource.pitch;
        audioSource.pitch = pitch;
        audioSource.PlayOneShot(clip);
        audioSource.pitch = prevPitch;
    }
    #endregion

    #region Gizmos
    void OnDrawGizmosSelected()
    {
        // mostra o raio onde procura zumbis
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, threatDetectRadius);

        // limiares de corrida
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, runEnterDistance);
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, runExitDistance);
    }
    #endregion
}
