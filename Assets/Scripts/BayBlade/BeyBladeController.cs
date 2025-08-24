using UnityEngine;


// BayBladecontroller.cs
// Controlador principal del bayblade
// recoge los controles de los bayblade.
public class BeyBladeController : MonoBehaviour
{

    [Header("Beyblade Stats")]
    public BeybladeDAta beybladeData;
    public float currentspinSpeed;
    public bool isAlive;



    [Header("Components")]
    private Rigidbody rb;
    private BeybladePhysics physics;
    private SpecialAttackSystem specialAttacks;


    //Restos metodos, Inicializar, update, TakeDamage, etc,

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
