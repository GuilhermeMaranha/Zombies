#region Using Statements
using UnityEngine;
#endregion

public class Health : MonoBehaviour
{
    #region Fields
    [SerializeField] int maxHP = 3;
    [SerializeField] Animator anim;
    [SerializeField] int hp;
    bool dead;

    public AudioClip dth;
    public AudioClip hit;

    public AudioSource aud;
    #endregion

    #region Unity Methods
    void Awake()
    {
        hp = maxHP;
    }
    #endregion

    #region Methods
    public void ApplyDamage(int amount)
    {
        if (dead) return;
        hp -= amount;
        aud.PlayOneShot(hit);
        if (hp <= 0) Die();
    }

    void Die()
    {
        aud.PlayOneShot(dth);
        dead = true;
        if (anim) anim.SetTrigger("Die");
        Collider[] cols = GetComponentsInChildren<Collider>();
        for (int i = 0; i < cols.Length; i++) cols[i].enabled = false;
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb) rb.isKinematic = true;
        Destroy(gameObject, 4f);
    }
    #endregion
}
