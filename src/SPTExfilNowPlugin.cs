// SPTExfilNow
// Original author: ragnaroks
// Original project: https://github.com/ragnaroks/SPTExfilNow
// SPT 4.1.2 compatibility adaptation: HTPGC
// Modified: 2026-08-18
// License: GNU Affero General Public License v3.0 (AGPL-3.0)
// This file is a modified version of the original project.

using BepInEx;
using BepInEx.Configuration;
using Comfort.Common;
using EFT;
using SPT.Reflection.Patching;
using SPT.Reflection.Utils;
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace SPTExfilNow
{
    [BepInPlugin("net.skydust.SPTExfilNowPlugin", "SPTExfilNow", "1.1.0")]
    [BepInProcess("EscapeFromTarkov")]
    public sealed class SPTExfilNowPlugin : BaseUnityPlugin
    {
        private static object _localGame;
        private bool _busy;
        private LocalGameSpawnPatch _spawnPatch;
        private ConfigEntry<KeyboardShortcut> _shortcut;

        private void Awake()
        {
            _shortcut = Config.Bind(
                "快捷键",
                "原地撤离",
                new KeyboardShortcut(KeyCode.Backslash, KeyCode.LeftControl),
                "按下后在战局任意位置以 Survived 状态撤离。默认：左 Ctrl + \\");

            _spawnPatch = new LocalGameSpawnPatch();
            Logger.LogInfo("SPTExfilNow 1.1.0 loaded for SPT 4.1.x");
        }

        private void Start()
        {
            _spawnPatch.Enable();
        }

        private void OnDestroy()
        {
            _spawnPatch?.Disable();
            _localGame = null;
        }

        private void Update()
        {
            if (_busy || _shortcut == null || !_shortcut.Value.IsUp())
                return;

            if (_localGame == null)
                return;

            _busy = true;
            try
            {
                ExfilNow();
            }
            catch (Exception e)
            {
                Logger.LogError($"原地撤离失败: {e}");
                Notify("原地撤离失败，请查看 BepInEx 日志");
            }
            finally
            {
                _busy = false;
            }
        }

        private void ExfilNow()
        {
            GameWorld gameWorld = Singleton<GameWorld>.Instance;
            if (gameWorld == null || gameWorld.MainPlayer == null)
                return;

            object controller = GetMemberValue(gameWorld, "ExfiltrationController");
            if (controller == null)
            {
                Notify("未找到撤离控制器");
                return;
            }

            string fraction = gameWorld.MainPlayer.Fraction.ToString();
            string collectionName = string.Equals(fraction, "Scav", StringComparison.OrdinalIgnoreCase)
                ? "ScavExfiltrationPoints"
                : "ExfiltrationPoints";

            IEnumerable points = GetMemberValue(controller, collectionName) as IEnumerable;
            if (points == null)
            {
                Notify("未找到可用撤离点");
                return;
            }

            object exfil = points.Cast<object>()
                .FirstOrDefault(IsActiveExfil);

            if (exfil == null)
            {
                Notify("当前没有可用撤离点");
                return;
            }

            string exfilName = GetExfilName(exfil);
            if (string.IsNullOrEmpty(exfilName))
                exfilName = "EXFIL";

            InvokeStop(_localGame, gameWorld.MainPlayer.ProfileId, exfilName);

            Logger.LogInfo($"原地撤离: {exfilName}");
            _localGame = null;
        }

        private static bool IsActiveExfil(object point)
        {
            if (point == null)
                return false;

            object value = GetMemberValue(point, "isActiveAndEnabled");
            if (value is bool b)
                return b;

            if (point is Behaviour behaviour)
                return behaviour.isActiveAndEnabled;

            return true;
        }

        private static string GetExfilName(object point)
        {
            object settings = GetMemberValue(point, "Settings");
            object rawName = settings != null ? GetMemberValue(settings, "Name") : null;
            if (rawName == null)
                return null;

            // EFT localized-string wrappers generally expose Localized().
            MethodInfo localized = rawName.GetType().GetMethod(
                "Localized",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                Type.EmptyTypes,
                null);

            if (localized != null)
            {
                object result = localized.Invoke(rawName, null);
                if (result != null)
                    return result.ToString();
            }

            return rawName.ToString();
        }

        private static void InvokeStop(object localGame, string profileId, string exfilName)
        {
            MethodInfo stop = localGame.GetType()
                .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(m => m.Name == "Stop")
                .FirstOrDefault(m =>
                {
                    ParameterInfo[] p = m.GetParameters();
                    return p.Length >= 3 && p[0].ParameterType == typeof(string);
                });

            if (stop == null)
                throw new MissingMethodException(localGame.GetType().FullName, "Stop");

            ParameterInfo[] parameters = stop.GetParameters();
            object[] args = new object[parameters.Length];
            args[0] = profileId;

            // Avoid a compile-time dependency on the 40743 ExitStatus enum type.
            Type exitStatusType = parameters[1].ParameterType;
            args[1] = exitStatusType.IsEnum
                ? Enum.Parse(exitStatusType, "Survived")
                : Activator.CreateInstance(exitStatusType);

            args[2] = exfilName;

            // Fill any new optional/default parameters SPT/EFT may add later.
            for (int i = 3; i < parameters.Length; i++)
            {
                args[i] = parameters[i].HasDefaultValue
                    ? parameters[i].DefaultValue
                    : (parameters[i].ParameterType.IsValueType
                        ? Activator.CreateInstance(parameters[i].ParameterType)
                        : null);
            }

            stop.Invoke(localGame, args);
        }

        private static object GetMemberValue(object obj, string name)
        {
            if (obj == null)
                return null;

            Type type = obj.GetType();
            PropertyInfo property = type.GetProperty(
                name,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (property != null)
                return property.GetValue(obj);

            FieldInfo field = type.GetField(
                name,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            return field?.GetValue(obj);
        }

        private static void Notify(string message)
        {
            // NotificationManagerClass is obfuscated across EFT builds; locate it by method name.
            Type managerType = PatchConstants.EftTypes.FirstOrDefault(t =>
                t.GetMethods(BindingFlags.Public | BindingFlags.Static)
                 .Any(m => m.Name == "DisplayMessageNotification"));

            MethodInfo display = managerType?
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(m => m.Name == "DisplayMessageNotification");

            if (display == null)
                return;

            ParameterInfo[] p = display.GetParameters();
            object[] args = new object[p.Length];
            if (p.Length > 0)
                args[0] = message;
            for (int i = 1; i < p.Length; i++)
            {
                args[i] = p[i].HasDefaultValue
                    ? p[i].DefaultValue
                    : (p[i].ParameterType.IsValueType ? Activator.CreateInstance(p[i].ParameterType) : null);
            }
            display.Invoke(null, args);
        }

        private sealed class LocalGameSpawnPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                Type localGameType = PatchConstants.LocalGameType;

                // Original mod patched LocalGame.Spawn. Keep the same semantic target,
                // but resolve LocalGame through SPT 4.1.x's own reflection helper.
                MethodInfo spawn = localGameType.GetMethod(
                    "Spawn",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                if (spawn == null)
                    throw new MissingMethodException(localGameType.FullName, "Spawn");

                return spawn;
            }

            [PatchPostfix]
            private static void Postfix(object __instance)
            {
                _localGame = __instance;
            }
        }
    }
}
