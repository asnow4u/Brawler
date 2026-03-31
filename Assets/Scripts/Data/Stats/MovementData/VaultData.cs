using UnityEngine;

[CreateAssetMenu(fileName = "Vault", menuName = "ScriptableObjects/SceneObject/Movement/Vault")]
public class VaultData : BaseMovementData
{
    public override bool IsValid()
    {
        return Animation != null;
    }
}

