using UnityEngine;

/// <summary>Identifies which laboratory socket accepts this grabbable key.</summary>
public sealed class LabKeyToken : MonoBehaviour
{
    [SerializeField] private int id;
    public int Id { get => id; set => id = value; }
}
