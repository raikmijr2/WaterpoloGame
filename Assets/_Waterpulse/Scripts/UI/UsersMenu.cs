using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Waterpulse.Config;
using Waterpulse.Core;
using Waterpulse.Gameplay;
using Waterpulse.Logic;

namespace Waterpulse.UI
{
    // Usuarios del menú: "Qui juga?" (lista para entrar, borrar un usuario o jugar como convidat), "Nou usuari"
    // (solo el nombre) y los textos del jugador actual (en el menú principal y su récord en las opciones).
    // No cambia de pantalla: avisa con eventos y MenuFlow decide adónde ir. Textos visibles en catalán
    public class UsersMenu : MonoBehaviour
    {
        [SerializeField] UsersConfigSO config;
        [SerializeField] ScoringConfigSO scoringConfig;

        [Header("Qui juga?")]
        [Tooltip("Plantilla (inactiva) que se clona por cada usuario")]
        [SerializeField] UserSlot slotTemplate;
        [SerializeField] Text emptyLabel;
        [SerializeField] Button newUserButton;
        [SerializeField] Button guestButton;

        [Header("Nou usuari")]
        [SerializeField] InputField nameInput;
        [SerializeField] Text nameError;
        [SerializeField] Button createButton;

        [Header("Esborrar usuari")]
        [SerializeField] Text deleteQuestion;
        [SerializeField] Button confirmDeleteButton;

        [Header("Menú principal y opciones")]
        [SerializeField] Text playerLabel;
        [SerializeField] Text recordLabel;
        [SerializeField] Button resetRecordButton;
        [SerializeField] Button confirmResetButton;

        [Header("Textos")]
        [SerializeField] string playerFormat = "Jugador: {0}";
        [SerializeField] string guestText = "Jugant com a convidat";
        [SerializeField] string recordFormat = "El teu rècord: {0} punts";
        [SerializeField] string noRecordText = "Encara no tens rècord";
        [SerializeField] string guestRecordText = "Els convidats no guarden rècord";
        [SerializeField] string deleteQuestionFormat = "Vols esborrar {0}?";
        [SerializeField] string fullFormat = "Ja hi ha {0} usuaris: esborra'n un";
        [SerializeField] string emptyNameText = "Escriu un nom";
        [SerializeField] string tooLongFormat = "Com a màxim {0} caràcters";
        [SerializeField] string invalidText = "El nom té caràcters no vàlids";
        [SerializeField] string duplicateText = "Aquest nom ja existeix";

        public event Action SignedIn;          // se ha elegido usuario o convidat
        public event Action DeleteRequested;   // hay que confirmar el borrado de un usuario
        public event Action UserDeleted;
        public event Action RecordReset;

        readonly List<UserSlot> slots = new List<UserSlot>();
        UserRecords records;
        UserProfile pendingDelete;

        // Lo que se selecciona al abrir "Qui juga?", por orden de preferencia: el primer usuario, "Nou usuari" o "convidat"
        public Selectable[] SelectionOrder => new Selectable[] { slots.Count > 0 ? slots[0].NameButton : null, newUserButton, guestButton };

        static bool HasUser => PlayerSession.HasChosen && !PlayerSession.IsGuest;

        void Awake()
        {
            records = new UserRecords(config, scoringConfig);
            slotTemplate.gameObject.SetActive(false);
            nameInput.characterLimit = config.maxNameLength;
            nameInput.onSubmit.AddListener(_ => Create());   // Enter en el campo del nombre
            createButton.onClick.AddListener(Create);
            guestButton.onClick.AddListener(SignInAsGuest);
            confirmDeleteButton.onClick.AddListener(ConfirmDelete);
            confirmResetButton.onClick.AddListener(ConfirmResetRecord);
#if UNITY_WEBGL && !UNITY_EDITOR
            WebGLInput.mobileKeyboardSupport = true;   // en el móvil, el campo del nombre abre el teclado del sistema
#endif
        }

        // Rehace la lista de "Qui juga?" con los usuarios guardados
        public void RefreshList()
        {
            foreach (var slot in slots)
            {
                slot.gameObject.SetActive(false);   // fuera de la cuadrícula ya, aunque Destroy espere al final del frame
                Destroy(slot.gameObject);
            }
            slots.Clear();

            foreach (var user in records.Registry.Users)
            {
                var slot = Instantiate(slotTemplate, slotTemplate.transform.parent);
                slot.name = "User_" + user.Id;
                slot.NameLabel.text = user.Name;
                var u = user;
                slot.NameButton.onClick.AddListener(() => SignIn(u));
                slot.DeleteButton.onClick.AddListener(() => RequestDelete(u));
                slot.gameObject.SetActive(true);
                slots.Add(slot);
            }
            emptyLabel.gameObject.SetActive(slots.Count == 0);
            newUserButton.interactable = !records.Registry.IsFull;
        }

        public void PrepareNewUser()
        {
            nameInput.text = string.Empty;
            nameError.text = records.Registry.IsFull ? string.Format(fullFormat, config.maxUsers) : string.Empty;
        }

        // Textos del jugador actual: en el menú principal y su récord en las opciones
        public void RefreshCurrent()
        {
            playerLabel.text = HasUser ? string.Format(playerFormat, PlayerSession.UserName) : guestText;
            var record = HasUser ? records.RecordOf(PlayerSession.UserId) : null;
            if (record == null) recordLabel.text = guestRecordText;
            else recordLabel.text = record.HasRecord ? string.Format(recordFormat, record.Best) : noRecordText;
            resetRecordButton.interactable = record != null && record.HasRecord;
        }

        void Create()
        {
            var check = records.Registry.TryCreate(nameInput.text, out var user);
            if (check == NameCheck.Ok)
            {
                SignIn(user);
                return;
            }
            nameError.text = ErrorText(check);
            nameInput.ActivateInputField();   // sigue escribiendo sin tener que volver a hacer clic
        }

        string ErrorText(NameCheck check)
        {
            switch (check)
            {
                case NameCheck.Full: return string.Format(fullFormat, config.maxUsers);
                case NameCheck.Empty: return emptyNameText;
                case NameCheck.TooLong: return string.Format(tooLongFormat, config.maxNameLength);
                case NameCheck.InvalidCharacters: return invalidText;
                case NameCheck.Duplicate: return duplicateText;
                default: return string.Empty;
            }
        }

        void SignIn(UserProfile user)
        {
            PlayerSession.SignIn(user.Id, user.Name);
            SignedIn?.Invoke();
        }

        void SignInAsGuest()
        {
            PlayerSession.SignInAsGuest();
            SignedIn?.Invoke();
        }

        void RequestDelete(UserProfile user)
        {
            pendingDelete = user;
            deleteQuestion.text = string.Format(deleteQuestionFormat, user.Name);
            DeleteRequested?.Invoke();
        }

        // Borra el usuario y su récord. Si era el de la sesión, ya no hay nadie elegido
        void ConfirmDelete()
        {
            if (pendingDelete != null)
            {
                records.Remove(pendingDelete.Id);
                if (HasUser && PlayerSession.UserId == pendingDelete.Id) PlayerSession.SignOut();
                pendingDelete = null;
            }
            UserDeleted?.Invoke();
        }

        void ConfirmResetRecord()
        {
            if (HasUser) records.RecordOf(PlayerSession.UserId).Clear();
            RefreshCurrent();
            RecordReset?.Invoke();
        }
    }
}
