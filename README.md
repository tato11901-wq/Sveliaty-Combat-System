# Sveliaty

**Sveliaty** es un juego táctico de estilo Roguelike y combates por turnos basado en habilidades, dados y la correcta gestión de recursos (cartas de afinidad, vida y turnos). 

Este repositorio utiliza el motor **Unity**, y su arquitectura está pensada de forma **escalable, modular y basada en eventos (Event-Driven)** para la separación de responsabilidades entre los sistemas lógicos y la interfaz de usuario (UI).

---

##  Arquitectura General

El código del proyecto se encuentra bajo `Assets/Scripts/` y está dividido en cinco grandes dominios y responsabilidades, lo cual es vital entender para navegar por el proyecto:

1. **`Core/`**: Contiene la capa superior de control de flujo del juego. Aquí residen los gestores que orquestan las transiciones de estado a gran escala y sistemas macro (el loop principal del juego, Boss Rush, maldiciones, progreso, etc.).
2. **`Combat/`**: Módulos exclusivos a gestionar *lo que ocurre dentro de una pelea* entre un jugador y un enemigo. Mecánicas de dados, afinidades, cálculos de poder, ataques y resoluciones de turno.
3. **`Player/`**: Contiene las estadísticas, inventario, vitalidad y utilidades del jugador. El `PlayerManager` es la fuente de la verdad para conocer la vida, cartas y puntuación actuales.
4. **`Data/`**: Información invariable en forma de Bases de Datos y `ScriptableObjects` (`EnemyData`, `AbilityData`, `CurseData`). Los gestores vienen aquí para comprobar stats base de los enemigos al spawnearlos. También contiene sistemas abstractos como el guardado de la *"Run"*.
5. **`UI/`**: Contiene exclusivamente las clases que actualizan el aspecto visual en pantalla. La UI es un **oyente pasivo**; no controla lógica. Mueve barras de vida, iconos y textos en base a eventos de C# enviados desde los otros módulos.

---

##  Flujo del Juego (Game Loop)

El núcleo del juego sigue la estructura de **Runs de tipo Boss Rush** con progresión de dificultad escalonada:

1. **Pantalla de Inicio (`GameManager`)**: Muestra la interfaz y da la opción de empezar o cargar una "Run" existente (recuperando los datos a través de `RunSaveManager`).
2. **Inicio del "Rush" (`BossRushManager`)**: Al empezar una nueva Run, se reinicia al jugador (`PlayerManager.InitializeForNewRun()`) y se entra en un bucle que dura un máximo de 20 combates.
3. **Flujo de Combate (`CombatManager`)**: 
   - El sistema elige un enemigo usando el `BestiaryManager` y su `EnemyDatabase`, ajustando la "Tier" o nivel del enemigo dependiendo de lo profundo que estemos en la **Run**.
   - El combate involucra usar intentos (`attemptsRemaining`), dados, cartas de afinidad (Fuerza, Agilidad, Destreza) y habilidades de `AbilityManager`. 
   - Tras cada turno, se resuelven habilidades del y el dado es modificado por multiplicadores de las afinidades y debilidades.
   - Posibles *Maldiciones* entran en acción gestionadas por `CurseManager`, insertando eventos aleatorios como dados *Gambler*, eliminación del escudo, u obstrucción mágica.
4. **Resolución de Combate (`CombatManager` -> `BossRushManager`)**:
   - **Victoria**: El jugador gana cartas recompensa, se llama al chequeo de habilidades para quizás desbloquear otras nuevas, y el contador de combates sigue (+1) hacia el combate número 20 (El gran jefe con esteroides x3).
   - **Derrota**: Desemboca en el evento de `GameOver`, activando paneles y limpiando el guardado local.
5. **Auto-Guardado (`RunSaveManager`)**: Todo se serializa a JSON de manera automática detrás de escena para detener o proteger caídas inesperadas y para restaurar progreso.

---

##  Patrones de Diseño Usados

Si planeas modificar el código, valora y sigue respetando las siguientes convenciones pre-existentes de este proyecto:

### 1. Sistema Basado en Eventos (Publisher / Subscriber)
Las clases de sistema disparan eventos con delegados globales para advertir al sistema.
**Ejemplo:** En lugar de `UIPlayerStats.UpdateText(100)`, el `PlayerManager` grita al vacío: `OnHealthChanged?.Invoke(nuevaVida, maximaVida);` y las clases de `UI/` que están suscritas actúan en consecuencia. 

### 2. Singleton Managers
El proyecto utiliza gestores persistentes de *Instancia Única*:
- `PlayerManager.Instance`
- `GameManager.Instance`
- `RunSaveManager.Instance`
Para no entorpecer el árbol de objetos de Unity con búsquedas (FindObjectOfType) innecesarias.

### 3. Modelado de Instancia Vs Referencia
Todo el balance de estadísticas base de enemigos viene de **`ScriptableObjects`**, para permitir el balance rápido desde el inspector de Unity, pero en tiempo de juego para no ensuciarlos se duplica la información a su clase **`Instance`**.
- Ej: Un enemigo usa como base la plantilla estadística de `EnemyData`, pero en la arena el combate existe un objeto de paso vivo de la clase `EnemyInstance`.

---

## 🛠 Entidades Principales (Para Navegación Rápida)

- `Assets/Scripts/Core/BossRushManager.cs`: Controla los niveles, spawneo y progresión de combates. Si quieres modificar la dificultad global, mira aquí.
- `Assets/Scripts/Combat/CombatManager.cs`: Control de ataques. Resolución de dados contra Threshold de victorias.
- `Assets/Scripts/Player/PlayerManager.cs`: Estadísticas actuales, cartas poseídas y curación.
- `Assets/Scripts/Data/RunSaveManager.cs`: Lógica de auto-guardar tu progreso de Rogue-like.
- `Assets/Scripts/Core/CurseManager.cs`: Riesgos por alargar los turnos. Maldiciones aleatorias. Eventos de roguelike puros.
- `Assets/Scripts/Core/habilityManager.cs`: Compra y uso de ataques pre-programados con costes exóticos.
---
