using UnityEngine;
using UnityEngine.UI;

namespace Waterpulse.UI
{
    // Una fila de "Qui juga?": el nombre (entra con ese usuario) y el botón para borrarlo.
    // UsersMenu clona una plantilla por cada usuario
    public class UserSlot : MonoBehaviour
    {
        [SerializeField] Button nameButton;
        [SerializeField] Text nameLabel;
        [SerializeField] Button deleteButton;

        public Button NameButton => nameButton;
        public Text NameLabel => nameLabel;
        public Button DeleteButton => deleteButton;
    }
}
