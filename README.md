# fino-backend

Backend for **Fino** — a multi-tenant SaaS platform that converts bank statements and receipts into structured CSV files, with tenant isolation and secure data handling.  
👉 [finotools.app](https://finotools.app)

## Architecture
### Application 
<img width="1783" height="882" alt="image" src="https://github.com/user-attachments/assets/33604b84-a81c-4c96-a082-dc896dc87931" />

<img width="1670" height="942" alt="image" src="https://github.com/user-attachments/assets/0edd111e-c5d4-4093-b803-060465b79f7d" />


---

## Demo Video
https://github.com/user-attachments/assets/ef17ddd5-fb9d-4c1a-b1f0-890fea3b0883


## 🚀 Tech Stack
- **.NET 10 + FastEndpoints** — REST API backend
- **PostgreSQL (Supabase)** — database and authentication
- **AWS (S3, SQS, Textract, EC2)** — file storage, queueing, OCR, and deployment
- **Terraform** — infrastructure as code
- **Docker** — containerization

---

## 🔑 Key Features
- Separate **public vs private** flows (storage + queues)
- Secure file upload → processing → CSV conversion
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
