<!-- Machine-translated; pending native-speaker review. See TRANSLATIONS.md. -->
---
# [Titre bref de la décision]

## Problème

Décrivez le problème de conception architecturale que vous traitez, sans laisser de doute sur la raison pour laquelle vous traitez ce problème maintenant. En suivant une approche minimaliste, traitez et documentez uniquement les problèmes qui doivent être traités aux différents points du cycle de vie.

## Décision

Énoncez clairement l'orientation de l'architecture, c'est-à-dire la position que vous avez retenue.

## Groupe

Vous pouvez utiliser un regroupement simple—comme intégration, présentation, données, et ainsi de suite—pour aider à organiser l'ensemble des décisions. Vous pourriez aussi utiliser une ontologie architecturale plus sophistiquée, comme celle de John Kyaruzi et Jan van Katwijk, qui inclut des catégories plus abstraites telles qu'événement, calendrier et localisation. Par exemple, en utilisant cette ontologie, vous regrouperiez sous « événement » les décisions traitant des occurrences où le système a besoin d'informations.

## Hypothèses

Décrivez clairement les hypothèses sous-jacentes de l'environnement dans lequel vous prenez la décision—coût, calendrier, technologie, et ainsi de suite. Notez que les contraintes environnementales (telles que les normes technologiques acceptées, l'architecture d'entreprise, les modèles couramment employés, et ainsi de suite) peuvent limiter les alternatives que vous envisagez.

## Contraintes

Capturez toute contrainte supplémentaire sur l'environnement que l'alternative choisie (la décision) pourrait imposer.

## Positions

Listez les positions (options viables ou alternatives) que vous avez envisagées. Celles-ci nécessitent souvent de longues explications, parfois même des modèles et des diagrammes. Ce n'est pas une liste exhaustive. Cependant, vous ne voulez pas entendre la question « Avez-vous pensé à... ? » lors d'une revue finale ; cela entraîne une perte de crédibilité et une remise en question d'autres décisions architecturales. Cette section aide aussi à s'assurer que vous avez entendu les opinions des autres ; énoncer explicitement d'autres opinions aide à rallier leurs défenseurs à votre décision.

## Argumentation

Expliquez pourquoi vous avez sélectionné une position, en incluant des éléments tels que le coût de mise en œuvre, le coût total de possession, le délai de mise sur le marché et la disponibilité des ressources de développement requises. Ceci est probablement aussi important que la décision elle-même.

## Implications

Une décision s'accompagne de nombreuses implications, comme le dénote le métamodèle REMAP. Par exemple, une décision peut introduire le besoin de prendre d'autres décisions, créer de nouvelles exigences ou modifier des exigences existantes ; imposer des contraintes supplémentaires à l'environnement ; exiger de renégocier la portée ou le calendrier avec les clients ; ou exiger une formation supplémentaire du personnel. Comprendre clairement et énoncer les implications de votre décision peut être très efficace pour obtenir l'adhésion et créer une feuille de route pour l'exécution architecturale.

## Décisions connexes

Il est évident que de nombreuses décisions sont liées ; vous pouvez les lister ici. Cependant, nous avons constaté qu'en pratique, une matrice de traçabilité, des arbres de décision ou des métamodèles sont plus utiles. Les métamodèles sont utiles pour montrer des relations complexes de façon diagrammatique (comme les modèles Rose).

## Exigences connexes

Les décisions doivent être guidées par les besoins métier. Pour démontrer la redevabilité, mettez explicitement en correspondance vos décisions avec les objectifs ou exigences. Vous pouvez énumérer ces exigences connexes ici, mais nous avons trouvé plus pratique de référencer une matrice de traçabilité. Vous pouvez évaluer la contribution de chaque décision architecturale à la satisfaction de chaque exigence, puis évaluer dans quelle mesure l'exigence est satisfaite à travers l'ensemble des décisions. Si une décision ne contribue pas à satisfaire une exigence, ne prenez pas cette décision.

## Artefacts connexes

Listez les documents d'architecture, de conception ou de périmètre connexes que cette décision impacte.

## Principes connexes

Si l'entreprise dispose d'un ensemble de principes convenus, assurez-vous que la décision est cohérente avec un ou plusieurs d'entre eux. Cela aide à garantir l'alignement entre les domaines ou systèmes.

## Notes

Comme le processus de prise de décision peut prendre des semaines, nous avons trouvé utile de consigner les notes et problèmes que l'équipe discute pendant le processus de socialisation.