using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System;

public class VendingMachineManager : MonoBehaviour
{
    //Declare Variables
    enum State { idle, enterID, collection }
    State currentState = State.idle;
    bool? usingCash = null;

    float paymentValue;
    float customerCash;
    float totalChange;

    string myQuery;
    List<string> itemsForPickup = new List<string>();

    [Header("Display Screen")]
    public Text errorMessageText;
    public Text promptText;
    public Text customerOrderText;
    public Text paymentMethodText;
    public GameObject idLabelText;

    [Header("Collection Animations")]
    public Animator collectedItemAnimation;
    public Animator collectedChangeAnimation;

    // Use this for initialization
    void Start()
    {
        //Update user interface values to database values
        DisplayDatabaseValues();

        //give the user a prompt
        promptText.text = "INSERT CASH OR USE CARD";
    }

    void DisplayDatabaseValues()
    {
        //Array of all item ID's
        string[] itemIDs = { "A1", "A2", "A3", "B1", "B2", "B3", "C1", "C2", "C3" };

        //Update each items interface values to the ones in the database
        foreach (string itemId in itemIDs)
        {
            //create query for ID
            myQuery = "SELECT * FROM Items WHERE itemID = '" + itemId + "'";
            //run and read the results from the query
            RunQuery(myQuery);
            DB.reader.Read();

            //Find the quantity text GameObject according to the ID
            Text quantityText = GameObject.Find(itemId + "_QuantityText").GetComponent<Text>();
            //Set the quantity text to the same value as the database
            quantityText.text = DB.reader.GetInt32(1).ToString();

            //Find the price text GameObject according to the ID
            Text priceText = GameObject.Find(itemId + "_priceText").GetComponent<Text>();
            //Set the price text to the same value as the database
            priceText.text = DB.reader.GetFloat(2).ToString("C");

            //close the database
            DB.CloseDB();
        }
    }

    public void SetPaymentValue(float value)
    {
        //This method is called from the payment buttons
        paymentValue = value;
    }

    public void InsertCashButton()
    {
        //checks if cash is selected and that the user is using cash
        if (paymentValue != 0f && usingCash != false && currentState != State.collection)
        {
            //sets payment method to cash
            usingCash = true;
            //adds the selected cash to the total cash
            customerCash += paymentValue;
            //show the payment method
            paymentMethodText.text = "PAYMENT: CASH (" + customerCash.ToString("C") + ")";
            //checks if the current state is not already 'enterID'
            if (currentState != State.enterID)
            {
                //sets current state to 'enterID'
                currentState = State.enterID;
                //give the user a prompt
                promptText.text = "PICK ITEM AND PRESS ENTER\r\n";
                idLabelText.SetActive(true);
            }
        }
    }

    public void UseCardButton()
    {
        //checks if the card is selected and the user is using card
        if (paymentValue == 0f && usingCash != true && currentState != State.collection)
        {
            //sets payment method to card
            usingCash = false;
            //show the payment method
            paymentMethodText.text = "PAYMENT: CARD";
            //checks if the current state is not already 'enterID'
            if (currentState != State.enterID)
            {
                //sets current state to 'enterID'
                currentState = State.enterID;
                //give the user a prompt
                promptText.text = "PICK ITEM AND PRESS ENTER\r\n";
                idLabelText.SetActive(true);
            }
        }
    }

    public void IdButtonPressed(string idValue)
    {
        //Checks if the vending machine is ready to accept an item ID and the order too big
        if (currentState == State.enterID && customerOrderText.text.Length < 2)
        {
            customerOrderText.text += idValue;
        }
    }

    public void EnterButton()
    {
        errorMessageText.text = "";

        //check if the vending machine state is 'enterID' and the customer order has 2 characters
        if (currentState == State.enterID && customerOrderText.text.Length == 2)
        {
            //create query to select an item using the customer order
            myQuery = "SELECT * FROM Items WHERE itemID = '" + customerOrderText.text + "'";
            //Run the query
            RunQuery(myQuery);

            //Check if the customer order is valid
            if (DB.reader.Read())
            {
                //check if the quantity is more than 0
                if(DB.reader.GetInt32(1) > 0)
                {
                    //check if the customer is using cash AND have enough money OR they are using card
                    if (usingCash == true && customerCash >= DB.reader.GetFloat(2) || usingCash == false)
                    {
                        //Purchase the item and pass through the item price
                        StartCoroutine(PurchaseItem(DB.reader.GetFloat(2)));
                        //Exit the method
                        return;
                    }
                    else //The user needs to insert more cash
                    {
                        //Tell the user they have not inserted enough cash
                        errorMessageText.text = "Insert more cash for item '" + customerOrderText.text + "'";
                    }
                }
                else //The quantity must be 0
                {
                    //the item needs to be restocked
                    errorMessageText.text = "'" + customerOrderText.text + "' needs to be restocked";
                }
            }
            else //The customer order is not valid
            {
                //Tell the user that their order is not valid
                errorMessageText.text = "'" + customerOrderText.text + "' is not a valid item number";
                customerOrderText.text = "";
            }
            //close the database
            DB.CloseDB();
        }
    }

    IEnumerator PurchaseItem(float itemPrice)
    {
        //close the database
        DB.CloseDB();
        //add item to list of pickup items
        itemsForPickup.Add(customerOrderText.text);
        //create query to take 1 away from the item quantity
        myQuery = "UPDATE Items SET itemQuantity = itemQuantity - 1 WHERE itemID = '" + customerOrderText.text + "'";
        //Run the query
        RunQuery(myQuery);
        //close the database
        DB.CloseDB();
        //Update the UI
        DisplayDatabaseValues();

        //tell the user they can collect their item
        promptText.text = "'" + customerOrderText.text + "' HAS BEEN PURCHASED\r\nDON'T FORGET TO COLLECT YOUR ITEM";

        if (usingCash == true) //Checks if using cash to calculate change
        {
            //calculate change
            float change = customerCash - itemPrice;
            //check if change is more than 0
            if (change > 0)
            {
                promptText.text += " AND CHANGE";
            }
            //add change value to the total change
            totalChange += change;
            //set customer cash and change to 0
            customerCash = 0;
            change = 0;
        }

        //set state to collection
        currentState = State.collection;
        //set default values
        usingCash = null;
        idLabelText.SetActive(false);
        customerOrderText.text = "";
        paymentMethodText.text = "";

        //Wait 4 seconds
        yield return new WaitForSeconds(4f);

        //set state to idle
        currentState = State.idle;
        //set prompt to default
        promptText.text = "INSERT CASH OR USE CARD";
    }

    public void DeleteButton()
    {
        //Checks if the vending machine is ready to accept an item ID and the order is not empty
        if (currentState == State.enterID && customerOrderText.text != "")
        {
            //deletes the last character from the string
            customerOrderText.text = customerOrderText.text.Remove(customerOrderText.text.Length - 1);
        }
    }

    public void CancelButton()
    {
        //Checks if the user is in the 'enterID' state
        if (currentState == State.enterID)
        {
            //Run the CancelPurchase() coroutine
            StartCoroutine(CancelPurchase());
        }
    }

    IEnumerator CancelPurchase()
    {
        //Set timer
        float cancelTime = 2f;
        //Tell the user the transaction has been cancelled
        promptText.text = "PURCHASE CANCELLED";
        //if cash is selected
        if (usingCash == true)
        {
            //Remind the user to collect their money from the machine
            promptText.text += "\r\nDON'T FORGET TO COLLECT YOUR CASH";
            //Add time to cancelTime
            cancelTime += 1.5f;
            //Add the customer cash to total change
            totalChange += customerCash;
            //Set customer cash to 0
            customerCash = 0f;
        }
        //Set state to collection
        currentState = State.collection;
        //set values back to default
        usingCash = null;
        customerOrderText.text = "";
        errorMessageText.text = "";
        paymentMethodText.text = "";
        idLabelText.SetActive(false);

        //Wait for user to read the screen
        yield return new WaitForSeconds(cancelTime);

        //Set state to idle
        currentState = State.idle;
        //Set prompt to default
        promptText.text = "INSERT CASH OR USE CARD";
    }

    public void RestockItemsButton()
    {
        //write a query to change all item quantities to 10
        myQuery = "UPDATE Items SET itemQuantity = 10";
        //run the query
        RunQuery(myQuery);
        //close the database
        DB.CloseDB();
        //Update the UI
        DisplayDatabaseValues();
    }

    public void CollectItemsButton()
    {
        //Runs the coroutine that displays the items
        StartCoroutine(CollectItems());
    }

    IEnumerator CollectItems()
    {
        //check the number of items in the collection list is more than 0
        if (itemsForPickup.Count > 0)
        {
            //move all of the strings from one list to another
            List<string> collectedItems = itemsForPickup;
            //Clear the original list
            itemsForPickup = new List<string>();

            //loop that shows each animation
            foreach(string item in collectedItems)
            {
                //change the sprite to the current item
                collectedItemAnimation.GetComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>(item);
                collectedItemAnimation.SetTrigger("showItem");
                
                //wait for the animation to end
                yield return new WaitForSeconds(2.25f);
            }
        }
    }

    public void CollectChangeButton()
    {
        //check if the total change is more than 0
        if (totalChange > 0)
        {
            //reference the text component
            Text collectedChangeText = collectedChangeAnimation.GetComponentInChildren<Text>();
            //change the text component
            collectedChangeText.text = "+" + totalChange.ToString("C");
            //set totalChange to 0
            totalChange = 0;
            //reference the rect transform
            RectTransform imageRectTransform = collectedChangeAnimation.GetComponent<RectTransform>();
            //change the width of the image based on how long the change text is
            imageRectTransform.sizeDelta = new Vector2(24f * collectedChangeText.text.Length, imageRectTransform.sizeDelta.y);
            //trigger the animation
            collectedChangeAnimation.SetTrigger("showChange");
        }
    }

    void RunQuery(string query)
    {
        //setup the path to the database
        string dbPath = "URI=file:" + Path.Combine(Application.streamingAssetsPath, "VendingMachine.db");

        //setup and open the conneciton to the database
        DB.Connect(dbPath);

        //run the query
        DB.RunQuery(query);
    }
}