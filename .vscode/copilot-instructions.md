# Instrucciones para GitHub Copilot en este proyecto frontend

## Reutilizar código existente antes de crear algo nuevo

Siempre que propongas una solución, primero debes buscar y reutilizar código que ya exista en este proyecto.

- Prioriza buscar implementaciones similares:
  - En el mismo archivo donde se está trabajando.
  - En archivos del mismo directorio.
  - En módulos, servicios o componentes relacionados dentro del proyecto.

Solo crea nuevas funciones, servicios, componentes o utilidades cuando no exista nada razonablemente reutilizable o adaptable.

No preguntes al usuario si ya existe algo similar: realiza tú mismo la validación usando el contexto disponible y actúa en consecuencia.


## Reglas para commits

Cuando recomiendes realizar un commit, debes seguir este procedimiento:

1. Asume siempre que el usuario utilizará:
   - `git add .` para agregar todos los cambios al staging area.

2. Antes de sugerir el mensaje de commit:
   - Revisa el contexto de los cambios realizados (diff disponible en el entorno de desarrollo).
   - Analiza la intención del cambio para generar un mensaje coherente y descriptivo.

3. Mensajes de commit:
   - Deben ser en **español**.
   - Deben ser **claros, breves y descriptivos**, como un título.
   - No usar prefijos como `feat:`, `fix:`, `chore:` ni otros similares.
   - No incluir cuerpo del commit salvo que el usuario lo solicite explícitamente.

Ejemplos válidos:
- `Actualizar validaciones del formulario de cliente`
- `Agregar componente para edición de contactos`
- `Corregir cálculo de totales en el modal de recursos`
