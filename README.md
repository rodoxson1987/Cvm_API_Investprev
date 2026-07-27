# 📊 CVM & B3 - API de Fundos de Investimento e Ações

API RESTful desenvolvida em **.NET 8** e **SQLite** para sincronização automática dos dados abertos da **CVM** (Comissão de Valores Mobiliários) e consulta de cotações de ações da **B3** (via Yahoo Finance).

---

## 🛠️ Tecnologias Utilizadas

* **.NET 8** (Minimal APIs)
* **Entity Framework Core** (Com provedor SQLite)
* **CsvHelper** (Parsing de arquivos CSV e extração de ZIPs da CVM)
* **Swagger UI / OpenAPI** (Documentação e testes interativos)

---

## 🏗️ Estrutura do Projeto

```text
CvmApi/
├── Data/
│   └── AppDbContext.cs           # Contexto do Banco de Dados
├── Models/
│   ├── FundoCadastro.cs          # Cadastro de Fundos (CVM)
│   ├── InformeDiario.cs          # Cotas e Patrimônio dos Fundos
│   ├── CompanhiaAberta.cs        # Cadastro de Ações/Empresas (CVM)
│   └── DemonstracaoFinanceira.cs # Balanços e DREs
├── Services/
│   └── CvmSyncService.cs         # Lógica de Download, Parsing e Cotações
├── Program.cs                    # Configuração e Endpoints da API
└── README.md                     # Documentação do Projeto


🚀 Como Executar o Projeto
Pré-requisitos
.NET 8 SDK instalado.

1.Passo a Passo

Clonar o repositório:

git clone [https://github.com/SEU-USUARIO/CvmApi.git](https://github.com/SEU-USUARIO/CvmApi.git)
cd CvmApi

2.Criar e aplicar as Migrations no Banco de Dados:

dotnet ef migrations add Inicial
dotnet ef database update

3.Executar a aplicação:

dotnet run

4.Acessar a documentação (Swagger UI):

Navegue até: http://localhost:5009/swagger


📌 Endpoints da API
1. Sincronização (POST)
POST /api/cvm/sincronizar/{dataStr}

Descrição: Baixa e processa os informes diários da CVM para a data informada.

Formatos aceitos: DD-MM-AAAA, DD/MM/AAAA ou AAAA-MM-DD.

POST /api/cvm/cadastros/sincronizar

Descrição: Baixa o cadastro geral de todos os Fundos de Investimento (cad_fi.csv).

POST /api/cvm/acoes/sincronizar

Descrição: Baixa o cadastro oficial de Companhias Abertas da CVM (cad_cia_aberta.csv).


2. Consultas (GET)
GET /api/cvm/informes/recentes

Descrição: Retorna a contagem total e os N registros mais recentes importados no banco.

GET /api/cvm/fundo/{cnpj}

Descrição: Retorna os dados cadastrais completos e a série histórica de cotas de um fundo pelo CNPJ.

GET /api/cvm/empresa/{busca}

Descrição: Busca companhias abertas por CNPJ, Código CVM ou Razão Social.

GET /api/cvm/acao/cotacao/{termo}

Descrição: Busca cotações diárias históricas de ações na B3 informando o Ticker (ex: PETR4, VALE3) ou o CNPJ da Empresa (ex: 33000167000101).

📝 Licença
Este projeto está licenciado sob a licença MIT. Sinta-se à vontade para utilizar, estudar e modificar!