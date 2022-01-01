using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemSpawnController : MonoBehaviour
{

    [SerializeField] private List<GameObject> prefabItems = new List<GameObject>();
    //NOTE: Later might categories the items
    //Waves would be based on these categories

    [Header("Spawn Range")]
    [SerializeField] private float minTime;
    [SerializeField] private float maxTime;
    
    private float timer = 0;

    //TODO: This would need to be grabbed from some sort of level manager (different levels would have different values)
    private float levelHeight = 10;
    private float levelLength = 14;

    [Header("Debug")]
    [SerializeField] private bool spawnItems;
    [SerializeField] private int playerCount;
    [SerializeField] private bool spawn;

    // Start is called before the first frame update
    void Start()
    {

    }


    void Update()
    {
        if (spawnItems)
        {
            timer -= Time.deltaTime;

            if (timer < 0)
            {
                SpawnItems();
                timer = Random.Range(minTime, maxTime);
                Debug.Log(timer);
            }
        }

        //DEBUG
        if (spawn)
        {
            spawn = false;
            SpawnItems();
        }
    }

    private void SpawnItems()
    {
        //TODO: Get number of players (probably part of multiplayer integration

        int itemCount = Random.Range(playerCount / 2, playerCount * 2);

        
        for(int i=0; i<itemCount; i++)
        {
            GameObject item = prefabItems[Random.Range(0, prefabItems.Count)];
            Instantiate(item, new Vector3(Random.Range(-levelLength, levelLength), levelHeight, 0), Quaternion.identity);
        }

    }


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
}
