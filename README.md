# 📈 CvmApi - API de Sincronização e Leitura de Informes Diários da CVM

API REST desenvolvida em **.NET 10** para automatizar o download, processamento e armazenamento dos relatórios e dados diários de fundos de investimento fornecidos pela **CVM (Comissão de Valores Mobiliários)**.

O projeto utiliza **SQLite** para persistência leve de dados sem necessidade de instalação de serviços externos, sendo totalmente portátil para ambientes corporativos com restrições de permissão.

---

## 🛠️ Tecnologias Utilizadas

* **.NET 10 (ASP.NET Core Web API):** Framework principal para construção de endpoints RESTful.
* **Entity Framework Core (EF Core) 9.0+:** ORM para abstração e comunicação com o banco de dados.
* **SQLite (`Microsoft.EntityFrameworkCore.Sqlite`):** Banco de dados relacional embutido em arquivo local (`.db`), eliminando a necessidade de servidores de banco.
* **CsvHelper:** Biblioteca para parsing e leitura de dados em arquivos `.csv` e arquivos `.zip` descompactados em memória.
* **Swashbuckle (Swagger):** Interface gráfica interativa para documentação e testes dos endpoints.

---

## 🏛️ Arquitetura e Decisões de Projeto

### 1. Modelo de Banco de Dados Local (Portabilidade)
Para evitar a necessidade de privilégios de administrador para instalar bancos de dados como PostgreSQL ou SQL Server nas máquinas da equipe, optou-se pelo **SQLite**. 
* Toda a base fica armazenada em um único arquivo local (`cvm_database.db`).
* O arquivo de banco é mantido no `.gitignore` por boas práticas.
* O comando `db.Database.Migrate()` executa na inicialização do sistema, garantindo a criação automática do banco e das tabelas na máquina de qualquer desenvolvedor ao clonar e rodar o projeto.

### 2. Suporte a Alterações no Schema da CVM
A CVM atualizou recentemente a nomenclatura dos cabeçalhos em seus arquivos CSV (ex: de `CNPJ_FUNDO` para `CNPJ_FUNDO_CLASSE`). Mapeamos esses campos via `CsvHelper` aceitando múltiplos apelidos, garantindo retrocompatibilidade para dados antigos e suporte a dados novos.

---

## 🚀 Como Executar o Projeto na Sua Máquina

### Pré-requisitos
* **SDK do .NET 10.0** (ou superior) instalado.
* VS Code, Visual Studio ou qualquer editor de sua preferência.

### Passo a Passo

1. **Clonar o Repositório:**
   ```bash
   git clone [https://github.com/SEU_USUARIO/cvm-api-dotnet.git](https://github.com/SEU_USUARIO/cvm-api-dotnet.git)
   cd cvm-api-dotnet

2. **Restaurar Dependências e Executar a API: 

Bash

dotnet run

Nota: Na primeira execução, o .NET criará automaticamente o arquivo do banco de dados cvm_database.db e aplicará as migrations.

3. Acessar a Interface de Testes (Swagger):

Abra o navegador e acesse:

http://localhost:5009/swagger


## 📡 Endpoints Disponíveis

1. Sincronização Diária da CVM
Rota: POST /api/cvm/sincronizar/{data}

Parâmetro: data no formato AAAA-MM-DD (ex: 2024-01-15)

Descrição: 1. Conecta-se aos servidores da CVM e baixa o arquivo .zip mensal contendo o histórico.
2. Extrai e lê o CSV em memória via streaming.
3. Trata duplicidades e sanitiza os CNPJs.
4. Salva no banco SQLite apenas os registros inéditos referente à data informada.

Exemplo de Resposta (200 OK):

JSON
{
  "mensagem": "Processado com sucesso!",
  "registrosNovos": 24850
}

📁 Estrutura do Projeto

'''

CvmApi/
├── Data/
│   └── AppDbContext.cs       # Contexto do Entity Framework para SQLite
├── Models/
│   └── InformeDiario.cs      # Entidade que representa os dados da cota/patrimônio
├── Services/
│   └── CvmSyncService.cs     # Serviço de download, extração do ZIP e parsing do CSV
├── Properties/
│   └── launchSettings.json   # Configurações do servidor Kestrel (Portas)
├── appsettings.json          # Connection String e configurações da aplicação
├── Program.cs                # Injeção de dependências e inicialização do Kestrel/Migrations
└── README.md                 # Documentação do repositório

'''

🔮 Próximos Passos (Roadmap)


[ ] Endpoint GET /api/cvm/fundo/{cnpj}: Consulta do histórico de cotas e patrimônio de um fundo específico.

[ ] Worker de Automação: Agendamento diário automático para buscar e salvar novos informes da CVM sem interações manuais.

[ ] Visualização de Dados: Implementação de visualizações e relatórios com os dados sincronizados.

---

