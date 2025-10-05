#region Using Statements
using UnityEngine;
using System.Collections;
#endregion

public class WeaponHitbox : MonoBehaviour
{
    #region Fields
    [SerializeField] int damage = 1;
    [SerializeField] string targetTag = "Untagged";
    Collider col;

    Coroutine autoDisableRoutine;
    #endregion

    #region Unity Methods
    void Awake()
    {
        col = GetComponent<Collider>();
        if (col) col.enabled = false;
        if (col) col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (targetTag != "Untagged" && !other.CompareTag(targetTag)) return;

        Health h = other.GetComponentInParent<Health>();
        if (!h) h = other.GetComponent<Health>();
        if (h) h.ApplyDamage(damage);
    }
    #endregion

    #region Methods
    public void EnableHitbox(bool on)
    {
        if (!col) return;

        // se habilitar/desabilitar manual, cancela auto-desligamento
        if (autoDisableRoutine != null)
        {
            StopCoroutine(autoDisableRoutine);
            autoDisableRoutine = null;
        }

        col.enabled = on;
    }

    public void EnableFor(float seconds)
    {
        if (!col) return;

        // reinicia ciclo
        if (autoDisableRoutine != null)
            StopCoroutine(autoDisableRoutine);

        col.enabled = true;
        autoDisableRoutine = StartCoroutine(AutoDisableAfter(seconds));
    }

    IEnumerator AutoDisableAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (col) col.enabled = false;
        autoDisableRoutine = null;
    }
    #endregion
}
