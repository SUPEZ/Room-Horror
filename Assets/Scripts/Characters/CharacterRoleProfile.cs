using MoreMountains.TopDownEngine;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Character))]
[AddComponentMenu("Room Horror/Characters/Character Role Profile")]
public class CharacterRoleProfile : MonoBehaviour
{
    [Header("Role")]
    [SerializeField] private CharacterRoleType _role = CharacterRoleType.Worker;

    private Character _character;
    private CharacterProvider _provider;
    private CharacterGatherer _gatherer;

    public CharacterRoleType Role => _role;
    public Character Character => _character;
    public CharacterProvider Provider => _provider != null ? _provider : (_provider = GetComponent<CharacterProvider>());
    public CharacterGatherer Gatherer => _gatherer != null ? _gatherer : (_gatherer = GetComponent<CharacterGatherer>());
    public bool IsAiControlled => _character != null && _character.CharacterType == Character.CharacterTypes.AI;
    public bool HasActiveTask => Gatherer != null && Gatherer.HasActiveTask;

    private void Awake()
    {
        RefreshCachedComponents();
    }

    private void OnEnable()
    {
        if (CharacterTaskManager.HasInstance)
        {
            CharacterTaskManager.Instance.RegisterCharacter(this);
        }
    }

    private void Start()
    {
        if (CharacterTaskManager.HasInstance)
        {
            CharacterTaskManager.Instance.RegisterCharacter(this);
        }
    }

    private void OnDisable()
    {
        if (CharacterTaskManager.HasInstance)
        {
            CharacterTaskManager.Instance.UnregisterCharacter(this);
        }
    }

    public void RefreshCachedComponents()
    {
        _character = GetComponent<Character>();
        _provider = GetComponent<CharacterProvider>();
        _gatherer = GetComponent<CharacterGatherer>();
    }

    public void SetRole(CharacterRoleType newRole)
    {
        _role = newRole;
    }

    public bool CanAcceptTask(CharacterTaskType taskType)
    {
        switch (_role)
        {
            case CharacterRoleType.Warrior:
                return taskType == CharacterTaskType.Combat;

            case CharacterRoleType.Scout:
                return taskType == CharacterTaskType.Scout;

            case CharacterRoleType.Worker:
                return taskType == CharacterTaskType.GatherResources || taskType == CharacterTaskType.Build;

            case CharacterRoleType.Scientist:
                return taskType == CharacterTaskType.Research;

            default:
                return false;
        }
    }
}
