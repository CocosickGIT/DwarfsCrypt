using System;
using UnityEngine.InputSystem;

namespace DwarfsCrypt.Presentation.Player
{
    public sealed class PlayerInputActions

    {
    private const string k_Json = @"{
            ""name"": ""PlayerInputActions"",
            ""maps"": [
                {
                    ""name"": ""GamePlayer"",
                    ""id"": ""a1b2c3d4-e5f6-4a7b-8c9d-0e1f2a3b4c5d"",
                    ""actions"": [
                        {
                            ""name"": ""Move"",
                            ""type"": ""Value"",
                            ""id"": ""b2c3d4e5-f6a7-4b8c-9d0e-1f2a3b4c5d6e"",
                            ""expectedControlType"": ""Vector2"",
                            ""processors"": """",
                            ""interactions"": """",
                            ""initialStateCheck"": true
                        },
                        {
                            ""name"": ""Dash"",
                            ""type"": ""Button"",
                            ""id"": ""c3d4e5f6-a7b8-4c9d-0e1f-2a3b4c5d6e7f"",
                            ""expectedControlType"": ""Button"",
                            ""processors"": """",
                            ""interactions"": """",
                            ""initialStateCheck"": false
                        },
                        {
                            ""name"": ""Skill1"",
                            ""type"": ""Button"",
                            ""id"": ""d4e5f6a7-b8c9-4d0e-1f2a-3b4c5d6e7f8a"",
                            ""expectedControlType"": ""Button"",
                            ""processors"": """",
                            ""interactions"": """",
                            ""initialStateCheck"": false
                        },
                        {
                            ""name"": ""Skill2"",
                            ""type"": ""Button"",
                            ""id"": ""e5f6a7b8-c9d0-4e1f-2a3b-4c5d6e7f8a9b"",
                            ""expectedControlType"": ""Button"",
                            ""processors"": """",
                            ""interactions"": """",
                            ""initialStateCheck"": false
                        },
                        {
                            ""name"": ""Skill3"",
                            ""type"": ""Button"",
                            ""id"": ""f6a7b8c9-d0e1-4f2a-3b4c-5d6e7f8a9b0c"",
                            ""expectedControlType"": ""Button"",
                            ""processors"": """",
                            ""interactions"": """",
                            ""initialStateCheck"": false
                        }
                    ],
                    ""bindings"": [
                        {
                            ""name"": ""WASD"",
                            ""id"": ""a7b8c9d0-e1f2-4a3b-4c5d-6e7f8a9b0c1d"",
                            ""path"": ""Dpad"",
                            ""interactions"": """",
                            ""processors"": """",
                            ""groups"": """",
                            ""action"": ""Move"",
                            ""isComposite"": true,
                            ""isPartOfComposite"": false
                        },
                        {
                            ""name"": ""up"",
                            ""id"": ""b8c9d0e1-f2a3-4b4c-5d6e-7f8a9b0c1d2e"",
                            ""path"": ""<Keyboard>/w"",
                            ""interactions"": """",
                            ""processors"": """",
                            ""groups"": ""Keyboard&Mouse"",
                            ""action"": ""Move"",
                            ""isComposite"": false,
                            ""isPartOfComposite"": true
                        },
                        {
                            ""name"": ""up"",
                            ""id"": ""c9d0e1f2-a3b4-4c5d-6e7f-8a9b0c1d2e3f"",
                            ""path"": ""<Keyboard>/upArrow"",
                            ""interactions"": """",
                            ""processors"": """",
                            ""groups"": ""Keyboard&Mouse"",
                            ""action"": ""Move"",
                            ""isComposite"": false,
                            ""isPartOfComposite"": true
                        },
                        {
                            ""name"": ""down"",
                            ""id"": ""d0e1f2a3-b4c5-4d6e-7f8a-9b0c1d2e3f4a"",
                            ""path"": ""<Keyboard>/s"",
                            ""interactions"": """",
                            ""processors"": """",
                            ""groups"": ""Keyboard&Mouse"",
                            ""action"": ""Move"",
                            ""isComposite"": false,
                            ""isPartOfComposite"": true
                        },
                        {
                            ""name"": ""down"",
                            ""id"": ""e1f2a3b4-c5d6-4e7f-8a9b-0c1d2e3f4a5b"",
                            ""path"": ""<Keyboard>/downArrow"",
                            ""interactions"": """",
                            ""processors"": """",
                            ""groups"": ""Keyboard&Mouse"",
                            ""action"": ""Move"",
                            ""isComposite"": false,
                            ""isPartOfComposite"": true
                        },
                        {
                            ""name"": ""left"",
                            ""id"": ""f2a3b4c5-d6e7-4f8a-9b0c-1d2e3f4a5b6c"",
                            ""path"": ""<Keyboard>/a"",
                            ""interactions"": """",
                            ""processors"": """",
                            ""groups"": ""Keyboard&Mouse"",
                            ""action"": ""Move"",
                            ""isComposite"": false,
                            ""isPartOfComposite"": true
                        },
                        {
                            ""name"": ""left"",
                            ""id"": ""a3b4c5d6-e7f8-4a9b-0c1d-2e3f4a5b6c7d"",
                            ""path"": ""<Keyboard>/leftArrow"",
                            ""interactions"": """",
                            ""processors"": """",
                            ""groups"": ""Keyboard&Mouse"",
                            ""action"": ""Move"",
                            ""isComposite"": false,
                            ""isPartOfComposite"": true
                        },
                        {
                            ""name"": ""right"",
                            ""id"": ""b4c5d6e7-f8a9-4b0c-1d2e-3f4a5b6c7d8e"",
                            ""path"": ""<Keyboard>/d"",
                            ""interactions"": """",
                            ""processors"": """",
                            ""groups"": ""Keyboard&Mouse"",
                            ""action"": ""Move"",
                            ""isComposite"": false,
                            ""isPartOfComposite"": true
                        },
                        {
                            ""name"": ""right"",
                            ""id"": ""c5d6e7f8-a9b0-4c1d-2e3f-4a5b6c7d8e9f"",
                            ""path"": ""<Keyboard>/rightArrow"",
                            ""interactions"": """",
                            ""processors"": """",
                            ""groups"": ""Keyboard&Mouse"",
                            ""action"": ""Move"",
                            ""isComposite"": false,
                            ""isPartOfComposite"": true
                        },
                        {
                            ""name"": """",
                            ""id"": ""d6e7f8a9-b0c1-4d2e-3f4a-5b6c7d8e9f0a"",
                            ""path"": ""<Gamepad>/leftStick"",
                            ""interactions"": """",
                            ""processors"": """",
                            ""groups"": ""Gamepad"",
                            ""action"": ""Move"",
                            ""isComposite"": false,
                            ""isPartOfComposite"": false
                        },
                        {
                            ""name"": """",
                            ""id"": ""e7f8a9b0-c1d2-4e3f-4a5b-6c7d8e9f0a1b"",
                            ""path"": ""<Keyboard>/space"",
                            ""interactions"": """",
                            ""processors"": """",
                            ""groups"": ""Keyboard&Mouse"",
                            ""action"": ""Dash"",
                            ""isComposite"": false,
                            ""isPartOfComposite"": false
                        },
                        {
                            ""name"": """",
                            ""id"": ""f8a9b0c1-d2e3-4f4a-5b6c-7d8e9f0a1b2c"",
                            ""path"": ""<Gamepad>/buttonSouth"",
                            ""interactions"": """",
                            ""processors"": """",
                            ""groups"": ""Gamepad"",
                            ""action"": ""Dash"",
                            ""isComposite"": false,
                            ""isPartOfComposite"": false
                        },
                        {
                            ""name"": """",
                            ""id"": ""a9b0c1d2-e3f4-4a5b-6c7d-8e9f0a1b2c3d"",
                            ""path"": ""<Keyboard>/1"",
                            ""interactions"": """",
                            ""processors"": """",
                            ""groups"": ""Keyboard&Mouse"",
                            ""action"": ""Skill1"",
                            ""isComposite"": false,
                            ""isPartOfComposite"": false
                        },
                        {
                            ""name"": """",
                            ""id"": ""b0c1d2e3-f4a5-4b6c-7d8e-9f0a1b2c3d4e"",
                            ""path"": ""<Keyboard>/2"",
                            ""interactions"": """",
                            ""processors"": """",
                            ""groups"": ""Keyboard&Mouse"",
                            ""action"": ""Skill2"",
                            ""isComposite"": false,
                            ""isPartOfComposite"": false
                        },
                        {
                            ""name"": """",
                            ""id"": ""c1d2e3f4-a5b6-4c7d-8e9f-0a1b2c3d4e5f"",
                            ""path"": ""<Keyboard>/3"",
                            ""interactions"": """",
                            ""processors"": """",
                            ""groups"": ""Keyboard&Mouse"",
                            ""action"": ""Skill3"",
                            ""isComposite"": false,
                            ""isPartOfComposite"": false
                        }
                    ]
                }
            ],
            ""controlSchemes"": [
                {
                    ""name"": ""Keyboard&Mouse"",
                    ""bindingGroup"": ""Keyboard&Mouse"",
                    ""devices"": [
                        { ""devicePath"": ""<Keyboard>"", ""isOptional"": false, ""isOR"": false },
                        { ""devicePath"": ""<Mouse>"", ""isOptional"": true, ""isOR"": false }
                    ]
                },
                {
                    ""name"": ""Gamepad"",
                    ""bindingGroup"": ""Gamepad"",
                    ""devices"": [
                        { ""devicePath"": ""<Gamepad>"", ""isOptional"": false, ""isOR"": false }
                    ]
                },
                {
                    ""name"": ""Touch"",
                    ""bindingGroup"": ""Touch"",
                    ""devices"": [
                        { ""devicePath"": ""<Touchscreen>"", ""isOptional"": false, ""isOR"": false }
                    ]
                }
            ]
        }";

    public InputAction Move { get; }
    public InputAction Dash { get; }
    public InputAction Skill1 { get; }
    public InputAction Skill2 { get; }
    public InputAction Skill3 { get; }

    private readonly InputActionAsset _asset;
    private readonly InputActionMap _map;

    public PlayerInputActions()
    {
        _asset = InputActionAsset.FromJson(k_Json);
        _map = _asset.FindActionMap("GamePlayer", throwIfNotFound: true);
        Move   = _map.FindAction("Move", throwIfNotFound: true);
        Dash   = _map.FindAction("Dash", throwIfNotFound: true);
        Skill1 = _map.FindAction("Skill1", throwIfNotFound: true);
        Skill2 = _map.FindAction("Skill2", throwIfNotFound: true);
        Skill3 = _map.FindAction("Skill3", throwIfNotFound: true);
    }

    public void Enable() => _map.Enable();

    public void Disable() => _map.Disable();
    // public void Dispose() => _asset.Dispose();
    }
}
