using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PaymentSelectionButton : MonoBehaviour {

    [SerializeField] float value;

    VendingMachineManager manager;

    PaymentSelectionButton[] buttons;


    void Awake()
    {
        manager = FindObjectOfType<VendingMachineManager>();
        buttons = FindObjectsOfType<PaymentSelectionButton>();

        if (value == 5f)
        {
            manager.SetPaymentValue(value);
            HighlightButton();
        }
    }

    void OnMouseDown()
    {
        //Set the payment value
        print(value.ToString("C") + " is selected.");
        manager.SetPaymentValue(value);
        //Highlight the button
        HighlightButton();
    }

    void HighlightButton()
    {
        foreach (PaymentSelectionButton button in buttons)
        {
            foreach (Transform child in button.transform)
            {
                if (child.gameObject.GetComponent<Image>() || child.gameObject.GetComponent<SpriteRenderer>())
                {
                    child.gameObject.SetActive(false);
                }
            }
        }

        foreach (Transform child in transform)
        {
            if (child.gameObject.GetComponent<Image>() || child.gameObject.GetComponent<SpriteRenderer>())
            {
                child.gameObject.SetActive(true);
            }
        }
    }

}
