using UnityEngine;

public class WeaponPickup : MonoBehaviour
{
    [Header("Qual arma desbloquear? (índice do array no ControlePersonagem)")]
    public int weaponIndex = 0;

    [Header("Comportamento")]
    public bool autoEquipOnPickup = true; // se true, equipa imediatamente ao coletar
}
