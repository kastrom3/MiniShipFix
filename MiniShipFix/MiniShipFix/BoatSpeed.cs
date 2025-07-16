using HarmonyLib;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using Vintagestory.API.Common; // Добавьте для ICoreAPI
using Vintagestory.API.Config;
using System;
using Vintagestory.API.Client;

namespace MiniShipFix
{
    [HarmonyPatch(typeof(EntityBoat))]
    public static class EntityBoatSpeedPatches
    {
        // Конфигурационные параметры
        public static float GetForwardSpeedMultiplier(this EntityBoat boat)
        {
            return boat.WatchedAttributes.GetFloat("forwardSpeedMultiplier", 1.0f); // Скорость движения вперёд
        }

        public static float GetBackwardSpeedMultiplier(this EntityBoat boat)
        {
            return boat.WatchedAttributes.GetFloat("backwardSpeedMultiplier", 1.0f); // Скорость движения назад
        }

        public static float GetAngularSpeedMultiplier(this EntityBoat boat)
        {
            return boat.WatchedAttributes.GetFloat("angularSpeedMultiplier", 1.0f); // Скорость поворота
        }
        public static float GetBowAngle(this EntityBoat boat)
        {
            return boat.WatchedAttributes.GetFloat("bowAngle", 5f); // Угол дифферента
        }

        public static float GetRollAngle(this EntityBoat boat)
        {
            return boat.WatchedAttributes.GetFloat("rollAngle", 5f); // Угол крена
        }

        // Загружаем параметры при инициализации лодки
        [HarmonyPostfix]
        [HarmonyPatch("Initialize")]
        public static void LoadSpeedMultipliers(EntityBoat __instance, EntityProperties properties, ICoreAPI api)
        {
            __instance.WatchedAttributes.SetFloat(
                "forwardSpeedMultiplier",
                properties.Attributes["forwardSpeedMultiplier"].AsFloat(1.0f)
            );

            __instance.WatchedAttributes.SetFloat(
                "backwardSpeedMultiplier",
                properties.Attributes["backwardSpeedMultiplier"].AsFloat(1.0f)
            );

            __instance.WatchedAttributes.SetFloat(
                "angularSpeedMultiplier",
                properties.Attributes["angularSpeedMultiplier"].AsFloat(1.0f)
            );
            __instance.WatchedAttributes.SetFloat(
                "bowAngle",
                properties.Attributes["bowAngle"].AsFloat(5f)
            );
            __instance.WatchedAttributes.SetFloat(
                "rollAngle",
                properties.Attributes["rollAngle"].AsFloat(5f)
            );

            // Логирование при прогрузке лодок/плотов
            api.World.Logger.Event($"[MiniShipFix] Speed initialize - EntityId: {__instance.EntityId}, Forward: {__instance.GetForwardSpeedMultiplier()}, Backward: {__instance.GetBackwardSpeedMultiplier()}, Angular: {__instance.GetAngularSpeedMultiplier}");
        }

        // Изменения движения лодки
        [HarmonyPrefix]
        [HarmonyPatch("updateBoatAngleAndMotion")]
        public static bool PrefixUpdateBoatAngleAndMotion(EntityBoat __instance, float dt)
        {
            // Логирование в начале
            // __instance.Api.World.Logger.Event($"[MiniShipFix] Before updateBoatAngleAndMotion - EntityId: {__instance.EntityId}, dt: {dt}, FwdSpeed: {__instance.ForwardSpeed}, AngVel: {__instance.AngularVelocity}, VanilSpeedMult: {__instance.SpeedMultiplier}");

            dt = GameMath.Min(0.5f, dt);
            float physicsFrameTime = GlobalConstants.PhysicsFrameTime;
            Vec2d vec2d = __instance.SeatsToMotion(physicsFrameTime); // Вызываем оригинальный метод для получения желаемого движения

            if (!__instance.Swimming)
            {
                return true; // Пусть ванильный метод продолжит, если лодка не в воде
            }

            // ПЕРЕПИСЫВАЕМ ЛОГИКУ ОБНОВЛЕНИЯ СКОРОСТИ
            double desiredForwardSpeed = vec2d.X;
            if (desiredForwardSpeed > 0)
            {
                desiredForwardSpeed *= __instance.GetForwardSpeedMultiplier();
            }
            else if (desiredForwardSpeed < 0)
            {
                desiredForwardSpeed *= __instance.GetBackwardSpeedMultiplier();
            }

            double desiredAngularVelocity = vec2d.Y * __instance.GetAngularSpeedMultiplier(); // ПРИМЕНЯЕМ МНОЖИТЕЛЬ ПОВОРОТА

            // Применяем множители для ForwardSpeed и AngularVelocity здесь
            // Игнорируем __instance.SpeedMultiplier, т.к. мы уже применили свои
            __instance.ForwardSpeed += (desiredForwardSpeed - __instance.ForwardSpeed) * dt;
            __instance.AngularVelocity += (desiredAngularVelocity - __instance.AngularVelocity) * dt; // ИСПОЛЬЗУЕМ ИЗМЕНЕННУЮ desiredAngularVelocity


            // Остальной код из ванильного updateBoatAngleAndMotion без изменений
            EntityPos sidedPos = __instance.SidedPos;
            if (__instance.ForwardSpeed != 0.0)
            {
                Vec3d vec3d = sidedPos.GetViewVector().Mul((float)(0.0 - __instance.ForwardSpeed)).ToVec3d();
                sidedPos.Motion.X = vec3d.X;
                sidedPos.Motion.Z = vec3d.Z;
            }

            EntityBehaviorPassivePhysicsMultiBox behavior = __instance.GetBehavior<EntityBehaviorPassivePhysicsMultiBox>();
            bool flag = true;
            if (__instance.AngularVelocity != 0.0)
            {
                float num = (float)__instance.AngularVelocity * dt * 30f;
                if (behavior.AdjustCollisionBoxesToYaw(dt, push: true, __instance.SidedPos.Yaw + num))
                {
                    sidedPos.Yaw += num;
                }
                else
                {
                    flag = false;
                }
            }
            else
            {
                flag = behavior.AdjustCollisionBoxesToYaw(dt, push: true, __instance.SidedPos.Yaw);
            }

            if (!flag)
            {
                if (behavior.AdjustCollisionBoxesToYaw(dt, push: true, __instance.SidedPos.Yaw - 0.1f))
                {
                    sidedPos.Yaw -= 0.0002f;
                }
                else if (behavior.AdjustCollisionBoxesToYaw(dt, push: true, __instance.SidedPos.Yaw + 0.1f))
                {
                    sidedPos.Yaw += 0.0002f;
                }
            }

            sidedPos.Roll = 0f;

            // Логирование в конце
            // __instance.Api.World.Logger.Event($"[MiniShipFix] After updateBoatAngleAndMotion - EntityId: {__instance.EntityId}, NewFwdSpeed: {__instance.ForwardSpeed}, NewAngVel: {__instance.AngularVelocity}");

            return false; // Отменяем выполнение оригинального метода updateBoatAngleAndMotion
        }

        [HarmonyPrefix]
        [HarmonyPatch("OnRenderFrame")]
        public static bool OnRenderFramePrefix(EntityBoat __instance, float dt, EnumRenderStage stage)
        {
            if (!(__instance.Api is ICoreClientAPI capi) || capi.IsGamePaused)
                return true;

            var traverse = Traverse.Create(__instance);
            var mountAngle = traverse.Field("mountAngle").GetValue<Vec3f>();
            float curRotMountAngleZ = traverse.Field("curRotMountAngleZ").GetValue<float>();

            // Оригинальный вызов обновления физики
            traverse.Method("updateBoatAngleAndMotion", dt).GetValue();

            if (__instance.Swimming)
            {
                // 1. Рассчитываем эффекты движения вперед/назад (mountAngle.Z)
                float speedSign = Math.Sign(__instance.ForwardSpeed);
                float speedFactor = GameMath.Clamp(Math.Abs((float)__instance.ForwardSpeed), 0, 1);
                float bowAngle = speedFactor * __instance.GetBowAngle() * GameMath.DEG2RAD * speedSign;

                // 2. Рассчитываем эффекты поворота (mountAngle.X)
                float turnSign = Math.Sign(__instance.AngularVelocity);
                float turnFactor = GameMath.Clamp(Math.Abs((float)__instance.AngularVelocity), 0, 1);
                float rollAngle = turnFactor * __instance.GetRollAngle() * GameMath.DEG2RAD * turnSign;

                // 3. Сохраняем оригинальную качку на волнах
                float waveX = GameMath.Sin((float)capi.InWorldEllapsedMilliseconds / 1000f) * 0.05f;
                float waveY = GameMath.Cos((float)capi.InWorldEllapsedMilliseconds / 2000f) * 0.05f;
                float waveZ = -GameMath.Sin((float)capi.InWorldEllapsedMilliseconds / 3000f) * 0.05f;

                // 4. Комбинируем эффекты
                mountAngle.X = (rollAngle * 10) + (waveX / 2); // Качка и крен
                mountAngle.Y = waveY / 2;    // Только качка
                mountAngle.Z = -bowAngle + (waveZ / 2);   // Качка и дифферент

                traverse.Field("mountAngle").SetValue(mountAngle);
            }

            // Применяем к рендереру
            if (__instance.Properties.Client.Renderer is EntityShapeRenderer renderer)
            {
                renderer.xangle = mountAngle.X;
                renderer.yangle = mountAngle.Y;
                renderer.zangle = mountAngle.Z + curRotMountAngleZ;
            }

            return false;
        }
    }
}