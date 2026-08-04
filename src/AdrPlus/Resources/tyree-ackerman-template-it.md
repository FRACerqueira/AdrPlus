<!-- Machine-translated; pending native-speaker review. See TRANSLATIONS.md. -->
---
# [Titolo breve della decisione]

## Problema

Descrivere il problema di progettazione architetturale che si sta affrontando, senza lasciare dubbi sul motivo per cui lo si sta affrontando ora. Seguendo un approccio minimalista, affrontare e documentare solo i problemi che devono essere affrontati nei vari punti del ciclo di vita.

## Decisione

Dichiarare chiaramente la direzione dell'architettura, ossia la posizione selezionata.

## Gruppo

È possibile usare un raggruppamento semplice—come integrazione, presentazione, dati e così via—per aiutare a organizzare l'insieme delle decisioni. È anche possibile usare un'ontologia architetturale più sofisticata, come quella di John Kyaruzi e Jan van Katwijk, che include categorie più astratte come evento, calendario e posizione. Ad esempio, usando questa ontologia, si raggrupperebbero sotto "evento" le decisioni che riguardano occorrenze in cui il sistema richiede informazioni.

## Presupposti

Descrivere chiaramente i presupposti di fondo nell'ambiente in cui si sta prendendo la decisione—costo, tempistiche, tecnologia e così via. Notare che i vincoli ambientali (come standard tecnologici accettati, architettura aziendale, pattern comunemente utilizzati e così via) potrebbero limitare le alternative considerate.

## Vincoli

Registrare eventuali vincoli aggiuntivi all'ambiente che l'alternativa scelta (la decisione) potrebbe imporre.

## Posizioni

Elencare le posizioni (opzioni valide o alternative) considerate. Queste spesso richiedono spiegazioni lunghe, a volte persino modelli e diagrammi. Non è un elenco esaustivo. Tuttavia, non si vuole sentire la domanda "Avete pensato a...?" durante una revisione finale; ciò porta a una perdita di credibilità e alla messa in discussione di altre decisioni architetturali. Questa sezione aiuta anche a garantire che le opinioni altrui siano state ascoltate; dichiarare esplicitamente le altre opinioni aiuta a coinvolgere i loro sostenitori nella decisione.

## Argomentazione

Delineare perché è stata selezionata una posizione, includendo elementi come il costo di implementazione, il costo totale di proprietà, il time to market e la disponibilità delle risorse di sviluppo necessarie. Questo è probabilmente importante quanto la decisione stessa.

## Implicazioni

Una decisione comporta molte implicazioni, come indica il metamodello REMAP. Ad esempio, una decisione potrebbe introdurre la necessità di prendere altre decisioni, creare nuovi requisiti o modificare requisiti esistenti; imporre vincoli aggiuntivi all'ambiente; richiedere una rinegoziazione dell'ambito o dei tempi con i clienti; oppure richiedere formazione aggiuntiva per il personale. Comprendere e dichiarare chiaramente le implicazioni della decisione può essere molto efficace per ottenere consenso e creare una tabella di marcia per l'esecuzione dell'architettura.

## Decisioni correlate

È evidente che molte decisioni sono correlate; è possibile elencarle qui. Tuttavia, abbiamo riscontrato che in pratica una matrice di tracciabilità, alberi decisionali o metamodelli risultano più utili. I metamodelli sono utili per mostrare relazioni complesse in forma diagrammatica (come i modelli Rose).

## Requisiti correlati

Le decisioni dovrebbero essere guidate dal business. Per dimostrare responsabilità, mappare esplicitamente le decisioni agli obiettivi o ai requisiti. È possibile elencare qui questi requisiti correlati, ma abbiamo riscontrato più conveniente fare riferimento a una matrice di tracciabilità. È possibile valutare il contributo di ciascuna decisione architetturale al soddisfacimento di ciascun requisito, per poi valutare quanto bene il requisito sia soddisfatto complessivamente da tutte le decisioni. Se una decisione non contribuisce al soddisfacimento di un requisito, non prenderla.

## Artefatti correlati

Elencare i documenti di architettura, progettazione o ambito correlati su cui questa decisione ha un impatto.

## Principi correlati

Se l'azienda dispone di un insieme di principi condivisi, assicurarsi che la decisione sia coerente con uno o più di essi. Questo aiuta a garantire l'allineamento tra domini o sistemi.

## Note

Poiché il processo decisionale può richiedere settimane, abbiamo riscontrato utile registrare note e problemi discussi dal team durante il processo di condivisione.