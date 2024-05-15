```mermaid
sequenceDiagram
    actor Systemleverandør
    participant Testmiljø
    participant Produksjonsmiljø
    actor HelseID-representant
    Systemleverandør->>Testmiljø: Registrerer systemet
    Systemleverandør->>Testmiljø: Verifiserer integrasjon mot HelseID
    Systemleverandør->>Testmiljø: Igangsetter produksjonssetting av systemet
    Testmiljø->>Produksjonsmiljø: Søknad opprettes i produksjonsmiljøet
    HelseID-representant-->>Systemleverandør: Kaller inn til kvalitetssikring
    HelseID-representant->>Produksjonsmiljø: Godkjenner systemet
```
