# fino-backend

Backend for **Fino** — a multi-tenant SaaS platform that converts bank statements and receipts into structured CSV files, with tenant isolation and secure data handling.  
👉 [finotools.app](https://finotools.app)

## Architecture
### Application 
<img width="1774" height="887" alt="fino-architecture" src="https://github.com/user-attachments/assets/007fcca1-0583-4634-b7ce-3d5a6b240f70" />

### Deployment 
<img width="1774" height="887" alt="image" src="https://github.com/user-attachments/assets/bc5b2271-8d3e-46f3-afe1-5b67da7ff198" />

---

## Demo Video
https://github.com/user-attachments/assets/ef17ddd5-fb9d-4c1a-b1f0-890fea3b0883


## 🚀 Tech Stack
- **.NET 10 + FastEndpoints** — REST API backend
- **PostgreSQL (Supabase)** — database and authentication
- **AWS (S3, SQS, Textract, ECS)** — file storage, queueing, OCR, and deployment
- **Terraform** — infrastructure as code
- **Docker** — containerization

---

## 🔑 Key Features
- Multi-tenant architecture with **subdomains** (`company.finotools.app`)
- Separate **public vs private** flows (storage + queues)
- Secure file upload → processing → CSV conversion
- Role-based tenant management
- CI/CD deployment to AWS

---

## 🛠️ Local Development

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download)
- [Docker](https://www.docker.com/)
- [Supabase CLI](https://supabase.com/docs/guides/cli)

### Setup
```bash
# clone repo
git clone https://github.com/markvu9/fino-backend.git
cd fino-backend

# restore packages
dotnet restore

# run locally
dotnet run
