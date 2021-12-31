using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class ItemController : MonoBehaviour
{

    public List<GameObject> prefabItems = new List<GameObject>();
    private Dictionary<int, Item> itemDictionary = new Dictionary<int, Item>();

    delegate void EffectPointer();
    
    private class Item
    {
        public int id;
        public string name;
        public GameObject go;

        public EffectPointer effect;

        public Item(int itemId, string itemName, GameObject itemGo)
        {
            id = itemId;
            name = itemName;
            go = itemGo;
        }
    }


    // Start is called before the first frame update
    void Start()
    {
        int i = 0;

        foreach(GameObject prefab in prefabItems)
        {
            Item newItem = new Item(i, prefab.name, prefab);
            SetUpItemEffect(newItem);
            itemDictionary.Add(i, newItem);

            i++;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void SetUpItemEffect(Item item)
    {
        switch (item.id)
        {
            case 0:
                item.effect += BombEffect;
                break;

            case 1:
                item.effect += HealingEffect;
                break;

            case 2:
                item.effect += RakeEffect;
                break;

            case 3:
                item.effect += DeckOfCardsEffect;
                break;

            case 4:
                item.effect += RunAwayChainsawEffect;
                break;

            case 5:
                item.effect += ExplosiveDiceEffect;
                break;

            case 6:
                item.effect += MorterEffect;
                break;

            case 7:
                item.effect += PropainTankEffect;
                break;

            default:
                break;
        }
    }


    private Item FindItem(GameObject go)
    {
        int i = 0;

        foreach (GameObject item in prefabItems) 
        {
            if (go == item)
            {
                return itemDictionary[i];
            }

            i++;
        }

        return null;
    }


    public void UseItem(GameObject go)
    {
        Item item = FindItem(go);

        item.effect();
    }



    //Healing item (food or hearts)
    //Sword
    //Explosive sheep/turtle (moving animel)
    //Gernade => soda bottle full of mentos
    //Sumbraros, frisbees that attach when thrown and make them dance
    //Hat weapon, boomerang weapon
    //Shrink ray
    //Vortex, vacume gernade
    //Card deck with 52 cards to shoot (52 pick up)
    //Dice - damage based on roll (gernade, random effects like gamin watch hammer, pick random gernade)
    //Coin - flip off screen then it return being huge
    //bottle of catnip - when thrown on someone they get swarmed by cats (constaint damage for a time)
    //steak - when thrown on someone they get swarmed by dogs
    //cheese - when thrown on someone they get swarmed by mouse
    //Spiky suit of armor - people take damage when they hit you / potentailly stick to walls
    //Moon boots - super jumps
    //Jet pack - floating
    //Giant fist - giant fist upgrade
    //paper airplane - when thrown if someone lands on it, it caries them with them
    //fish numchucks -
    //Big cat summon - Summoned cat will run to a fish, then roll off screen
    //Ball and chain - throwable to make them move slower and limit jump
    //Puffer fish - slow damage constent damage that can be traded off to anyone that touches them
    //GameControler - Reverses controlls for all players besides the one who picked it up
    //Ice cube - Throwable - makes an area slidy
    //Rollerscates - Faster movement speed
    //Rocket fists - faster moving fists
    //Reflective Armor - reflects punches and perjectiles
    //Wistle - summon big dude that trys to carry closest enemey off
    //RAKE!!! - land mine alternitive (THIS IS HAPPENING)
    //PB&J Sandwich - muffles any sounds that they make
    //Meteor staff - summon small meteors rain down to a location
    //Cannon that launchs sharks that attack nearby people
    //Runaway Chainsaw, when thrown and hits a platform it will travel the platform (like the fire sun item in smash)
    //Propain tank - blows when struck to many times
    //Explosive potato - timed bomb that can be punched around (microwave noice at the end)
    //Butterfly - tornado effect on the map
    //Sentry turret
    //Morter shell - rain morters random spots
    //Vacume cleaner - suck or spit out
    //Standing fan - weapon that hits multiple times
    //Cat with butter toat taped to its back - ultimate weapon
    //Jugeling pins - throw one at a time reducing the number your jugling, does damage also when your jugeling
    //Remote control drone - explodes on impact
    //Gitar - spreads music notes that hurt people
    //Bottle of glue - creats surface that will hold somebody there
    //windup car - Charge up, the more its windup the faster and stronger it is
    //Tree seed - grows into an oak, timbers after a time
    //lead boots - dont get knocked away as easy
    //Sticky boots - go on the underside of platform (like the fire sun item in smash)
    //Poket sand - throw at enemy to stun them

    private void HealingEffect()
    {

    }

    private void ExplosiveDiceEffect()
    {

    }

    private void DeckOfCardsEffect()
    {

    }

    private void RunAwayChainsawEffect()
    {

    }

    private void PropainTankEffect()
    {

    }

    private void MorterEffect()
    {

    }

    private void RakeEffect()
    {

    }
    
    private void BombEffect()
    {

    }
}
