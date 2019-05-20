using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CharController : MonoBehaviour
{
    [Range(1f, 5f)]
    [Tooltip("Установите скорость = 4")]
    public float SpeedMove = 4f;

    [Range(1f, 3f)]
    [Tooltip("Установите коэф. ускорения = 2")]
    public float AccelerationFactor = 2f;

    [Range(1f, 3f)]
    [Tooltip("Установите время ускорения = 1")]
    public float TimeMegaSpeed = 2f;

    [Range(1f, 10f)]
    [Tooltip("Установите коэф. соударения = 5")]
    public float ImpactFactor = 5f;

    [Tooltip("добавьте материалы для очернения после огня")]
    public Material[] MaterialsDark;

    [Tooltip("установите цвета по Defaulte для MaterialsDark")]
    public Color[] MatColorDark;


    private float _speedEuler;// скорость уклона дрона
    private float _impactForce;// сила соударения двух колизий
    private float _timeDamage, _timeMegaSdpeed;// отсчет времени удара и ускорения.
    private int _countDamage;// кол-во ударов 3

    private GameObject[] ParticlesGO;// эффекты
    private Rigidbody rigid;
    private Vector3 moveDrone;
    private bool activeDamage = false;
    private bool megaSpeed = false;

    private void Start()
    {
        Application.targetFrameRate = 60;
        _timeMegaSdpeed = TimeMegaSpeed;
        _impactForce = SpeedMove * ImpactFactor;
        _speedEuler = SpeedMove * 15f;
        _timeDamage = 7f;
        _countDamage = 0;
        activeDamage = false;
        // при старет переназначение цвета, если этого не будет то, после пламени и рестарта сцены цвета не вернуться в изначальное состояние.
        for (int i = 0; i < MaterialsDark.Length; i++)
        {
            MaterialsDark[i].color = MatColorDark[i];
        }
        // автоматический поиск партиклов и их откл.
        ParticlesGO = GameObject.FindGameObjectsWithTag("Particle");
        for (int i = 0; i < ParticlesGO.Length; i++)
        {
            ParticlesGO[i].SetActive(false);
        }

        rigid = GetComponent<Rigidbody>();
        // фризим позиции чтобы дрон не переворачивался
        if (rigid != null)
        {
            rigid.constraints = RigidbodyConstraints.FreezeRotation;
            rigid.useGravity = false;
            rigid.mass = 3;
            rigid.drag = 1;
            rigid.angularDrag = 10;
        }
        else
        {
            Debug.LogWarningFormat("Pls. Add a Rigidbody component to this GameObject");
        }
    }

    private void FixedUpdate()
    {
        if (!activeDamage)
        {
            if (rigid != null)
            {
                Move();
                RotateDrone();
            }
        }
    }
    private void Update()
    {
        if (activeDamage)
        {
            Particles();
        }
        if (Input.GetKey(KeyCode.R))
        {
            SceneManager.LoadSceneAsync(0, LoadSceneMode.Single);
        }
    }
    
    // move Drine 
    private void Move()
    {
        moveDrone = Vector3.zero;
        moveDrone.x = Input.GetAxis("Horizontal") * SpeedMove;
        moveDrone.z = Input.GetAxis("Vertical") * SpeedMove;
        rigid.AddForce(moveDrone * SpeedMove * Time.fixedDeltaTime, ForceMode.VelocityChange);
        // полет вверх
        if (Input.GetKey(KeyCode.Space))
        {
            rigid.AddForce(Vector3.up * SpeedMove * Time.fixedDeltaTime, ForceMode.VelocityChange);
        }
        // полет вниз
        if (Input.GetKey(KeyCode.LeftControl))
        {
            rigid.AddForce(Vector3.down * SpeedMove * Time.fixedDeltaTime, ForceMode.VelocityChange);
        }
        // ускорение
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            if (!megaSpeed)
            {
                SpeedMove *= AccelerationFactor;
            }
            megaSpeed = true;
        }
        Acceleration();
    }
    private void Acceleration()
    {
        if (megaSpeed)
        {
            if (_timeMegaSdpeed > 0)
            {
                _timeMegaSdpeed -= Time.deltaTime;
            }
            if (_timeMegaSdpeed < 0)
            {
                SpeedMove /= AccelerationFactor;
                megaSpeed = false;
                _timeMegaSdpeed = TimeMegaSpeed;
            }
        }
    }
    private void RotateDrone()
    {
        if (moveDrone.z != 0 || moveDrone.x != 0)
        {
            // отклонение rotation дрона.
            transform.localRotation = Quaternion.RotateTowards(transform.localRotation, Quaternion.Euler((moveDrone.z * 20f) / SpeedMove, 0.0f, (moveDrone.x * -20f) / SpeedMove), _speedEuler * Time.fixedDeltaTime);
        }
        else
        {   // возврат положения rotation для выравнивания дрона если координаты moveDrone==vector3.zero.
            transform.localRotation = Quaternion.RotateTowards(transform.localRotation, Quaternion.Euler(0f, 0f, 0f), _speedEuler * Time.fixedDeltaTime);
        }
    }
    private void OnCollisionEnter(Collision collision)// здесь хп
    {
        // определяем силу удара и если она > коэф*скорость то увеличиваем кол.дамаг
        // урезаем скорость движение и отклонения ипереназначем силу удара т.к.та была вызвана в старте.
        if (collision.relativeVelocity.magnitude > _impactForce)
        {
            _countDamage += 1;
            if (_countDamage == 1)// первый удар  
            {
                SpeedMove /= 1.5f;
                _speedEuler = SpeedMove * 15f;
                _impactForce = SpeedMove * ImpactFactor;
                ParticlesGO[0].SetActive(true);// первый эффект
            }
            if (_countDamage == 2)// второй
            {
                SpeedMove /= 1.5f;
                _speedEuler = SpeedMove * 15f;
                _impactForce = SpeedMove * 2f;
                ParticlesGO[1].SetActive(true);// второй эффект
            }
            if (_countDamage == 3)// третий смерть
            {
                rigid.angularDrag = 0.5f;
                rigid.useGravity = true;
                rigid.constraints = RigidbodyConstraints.None;
                rigid.AddTorque(new Vector3(-1f, -1f, 0f) * 1000f, ForceMode.Impulse);// тип сбит и падает как самолет кружась
                activeDamage = true;
            }
            Debug.Log(collision.relativeVelocity.magnitude);
        }
    }
    private void Particles()
    {
        if (_timeDamage > 0)
        {
            _timeDamage -= Time.deltaTime;
        }
        if (_timeDamage <= 0)
        {
            ParticlesGO[2].SetActive(true);
            for (int i = 0; i < MaterialsDark.Length; i++)
            {
                MaterialsDark[i].color = Color.Lerp(MaterialsDark[i].color, Color.black, Time.deltaTime * 1f);
            }
        }

    }

}
