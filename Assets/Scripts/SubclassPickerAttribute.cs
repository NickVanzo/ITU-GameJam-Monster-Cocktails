using UnityEngine;

// Put on a [SerializeReference] field to pick its class from a dropdown in the inspector.
// The field's type sets the choices: a GunTrigger field lists every [Serializable] GunTrigger.
// On a list, every element gets its own dropdown.
public class SubclassPickerAttribute : PropertyAttribute
{
}
