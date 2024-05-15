# System
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

# API
```mermaid
sequenceDiagram
    actor Tjenesteleverandør
    participant Testmiljø
    participant Produksjonsmiljø
    actor HelseID-representant
    Tjenesteleverandør->>Testmiljø: Registrerer API-et
    Tjenesteleverandør->>Testmiljø: Verifiserer integrasjon mot HelseID
    Tjenesteleverandør->>Testmiljø: Igangsetter produksjonssetting av API-et
    Testmiljø->>Produksjonsmiljø: Søknad opprettes i produksjonsmiljøet
    HelseID-representant-->>Tjenesteleverandør: Kaller inn til kvalitetssikring
    HelseID-representant->>Produksjonsmiljø: Godkjenner API-et
```
