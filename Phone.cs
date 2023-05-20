using GTA;
using iFruitAddon2;

namespace OnlineCab
{
    public class Phone : Script
    {
        internal static CustomiFruit _iFruit;

        internal static void SetupPhone ()
        {
            _iFruit = new CustomiFruit();

            iFruitContactCollection iFruitContacts = new iFruitContactCollection();

            iFruitContact cabContact = new iFruitContact("Pegasus Cab");
            cabContact.Answered += (iFruitContact contact) => OpenMenu();
            cabContact.DialTimeout = 1000;            // Delay before answering
            cabContact.Active = true;                 // true = the contact is available and will answer the phone
            cabContact.Icon = ContactIcon.Pegasus;   // Contact's icon
            _iFruit.Contacts.Add(cabContact);         // Add the contact to the phone
        }

        private static void OpenMenu ()
        {
            Menu.menu.Visible = true; 
            _iFruit.Close();
        }

    }
}
