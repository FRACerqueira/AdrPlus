<!-- Machine-translated; pending native-speaker review. See TRANSLATIONS.md. -->
---
# [Título breve de la decisión]

## Problema

Describa el problema de diseño arquitectónico que está abordando, sin dejar dudas sobre por qué está abordando este problema ahora. Siguiendo un enfoque minimalista, aborde y documente solo los problemas que necesitan abordarse en varios puntos del ciclo de vida.

## Decisión

Declare claramente la dirección de la arquitectura, es decir, la posición que ha seleccionado.

## Grupo

Puede usar una agrupación simple, como integración, presentación, datos, etc., para ayudar a organizar el conjunto de decisiones. También podría usar una ontología de arquitectura más sofisticada, como la de John Kyaruzi y Jan van Katwijk, que incluye categorías más abstractas como evento, calendario y ubicación. Por ejemplo, usando esta ontología, agruparía las decisiones que tratan con eventos en los que el sistema requiere información bajo evento.

## Supuestos

Describa claramente los supuestos subyacentes en el entorno en el que está tomando la decisión: costo, cronograma, tecnología, etc. Tenga en cuenta que las restricciones del entorno (como estándares tecnológicos aceptados, arquitectura empresarial, patrones comúnmente empleados, etc.) podrían limitar las alternativas que considere.

## Restricciones

Capture cualquier restricción adicional al entorno que la alternativa elegida (la decisión) pueda imponer.

## Posiciones

Enumere las posiciones (opciones viables o alternativas) que consideró. Estas a menudo requieren explicaciones largas, a veces incluso modelos y diagramas. Esta no es una lista exhaustiva. Sin embargo, no querrá escuchar la pregunta "¿Pensó en...?" durante una revisión final; esto lleva a la pérdida de credibilidad y al cuestionamiento de otras decisiones arquitectónicas. Esta sección también ayuda a garantizar que escuchó las opiniones de otros; declarar explícitamente otras opiniones ayuda a sumar a sus defensores a su decisión.

## Argumento

Describa por qué seleccionó una posición, incluyendo elementos como el costo de implementación, el costo total de propiedad, el tiempo de llegada al mercado y la disponibilidad de los recursos de desarrollo requeridos. Esto es probablemente tan importante como la decisión misma.

## Implicaciones

Una decisión conlleva muchas implicaciones, como denota el metamodelo REMAP. Por ejemplo, una decisión podría introducir la necesidad de tomar otras decisiones, crear nuevos requisitos o modificar requisitos existentes; imponer restricciones adicionales al entorno; requerir renegociar el alcance o el cronograma con los clientes; o requerir capacitación adicional del personal. Comprender claramente y declarar las implicaciones de su decisión puede ser muy eficaz para ganar aceptación y crear una hoja de ruta para la ejecución de la arquitectura.

## Decisiones relacionadas

Es obvio que muchas decisiones están relacionadas; puede enumerarlas aquí. Sin embargo, hemos descubierto que en la práctica, una matriz de trazabilidad, árboles de decisión o metamodelos son más útiles. Los metamodelos son útiles para mostrar relaciones complejas de forma diagramática (como los modelos Rose).

## Requisitos relacionados

Las decisiones deben estar impulsadas por el negocio. Para mostrar responsabilidad, mapee explícitamente sus decisiones a los objetivos o requisitos. Puede enumerar estos requisitos relacionados aquí, pero hemos descubierto que es más conveniente hacer referencia a una matriz de trazabilidad. Puede evaluar la contribución de cada decisión arquitectónica al cumplimiento de cada requisito y luego evaluar qué tan bien se cumple el requisito en todas las decisiones. Si una decisión no contribuye al cumplimiento de un requisito, no tome esa decisión.

## Artefactos relacionados

Enumere los documentos de arquitectura, diseño o alcance relacionados que esta decisión impacta.

## Principios relacionados

Si la empresa tiene un conjunto de principios acordado, asegúrese de que la decisión sea coherente con uno o más de ellos. Esto ayuda a garantizar la alineación entre dominios o sistemas.

## Notas

Debido a que el proceso de toma de decisiones puede llevar semanas, hemos descubierto que es útil capturar notas y problemas que el equipo discute durante el proceso de socialización.