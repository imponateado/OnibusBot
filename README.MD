# OnibusBot 🚌

*A Telegram bot for tracking bus positions and sending real-time notifications*

[English](#english) | [Português](#português)

---

## English

### Overview

OnibusBot is a Telegram bot designed to help users track public bus positions in real-time. The bot provides location updates and periodic notifications for selected bus lines.

### Features

- 🚌 **Real-time bus tracking**: Get current positions of buses of specific lines
- ⏰ **Periodic notifications**: Get updates every 2 minutes for subscribed lines
- 🔄 **Direction selection**: Choose between outbound ("IDA") and return ("VOLTA") directions
- 🛑 **Easy unsubscribe**: Stop notifications with a simple button click
- 🖥️ **Cross-platform daemon**: Runs as a service on both Linux (systemd) and Windows

### Prerequisites

- .NET 6.0 or later
- Telegram Bot Token (obtained from [@BotFather](https://t.me/botfather))

### Installation

1. **Clone the repository:**
   ```bash
   git clone https://github.com/imponateado/OnibusBot.git
   cd OnibusBot
   ```

2. **Set up your bot token:**
   
   **Option A: Environment Variable**
   ```bash
   export BOT_TOKEN="your_telegram_bot_token_here"
   ```
   
   **Option B: .env file**
   Create a `.env` file in the project root:
   ```
   BOT_TOKEN=your_telegram_bot_token_here
   ```

3. **Build the project:**
   ```bash
   dotnet build
   ```

4. **Run the bot:**
   ```bash
   dotnet run
   ```

### Running as a Service

#### Linux (systemd)

1. **Build for production:**
   ```bash
   dotnet publish -c Release -o /opt/onibusbot
   ```

2. **Create systemd service file** (`/etc/systemd/system/onibusbot.service`):
   ```ini
   [Unit]
   Description=OnibusBot Telegram Service
   After=network.target

   [Service]
   Type=notify
   ExecStart=/opt/onibusbot/OnibusBot
   Restart=always
   User=onibusbot
   Environment=BOT_TOKEN=your_token_here

   [Install]
   WantedBy=multi-user.target
   ```

3. **Enable and start the service:**
   ```bash
   sudo systemctl enable onibusbot
   sudo systemctl start onibusbot
   ```

#### Windows Service

1. **Build for production:**
   ```cmd
   dotnet publish -c Release -o C:\OnibusBot
   ```

2. **Install as Windows Service:**
   ```cmd
   sc create OnibusBot binPath="C:\OnibusBot\OnibusBot.exe"
   sc start OnibusBot
   ```

### How to Use

1. **Start a conversation** with your bot on Telegram
2. **Send `/start`** or say "oi" to begin
3. **Enter a bus line number** (e.g., "175")
4. **Select the exact line** from the available options
5. **Choose direction**: "IDA" (outbound) or "VOLTA" (return)
6. **Receive notifications** every 2 minutes with bus locations
7. **Stop notifications** anytime using the "⌐ Parar notificações" button

### Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Submit a pull request

### License

This project is open source. Check the repository for license details.

### Support

If you encounter any issues or have suggestions, open an issue on the [GitHub repository](https://github.com/imponateado/OnibusBot).

---

## Português

### Visão Geral

OnibusBot é um bot do Telegram projetado para ajudar usuários a acompanhar posições de ônibus públicos em tempo real. O bot fornece atualizações de localização e notificações periódicas para linhas de ônibus selecionadas.

### Funcionalidades

- 🚌 **Rastreamento em tempo real**: Obtenha posições atuais dos ônibus em linhas específicas
- ⏰ **Notificações periódicas**: Receba atualizações a cada 2 minutos para rotas inscritas
- 🔄 **Seleção de direção**: Escolha entre direções "IDA" e "VOLTA"
- 🛑 **Cancelamento fácil**: Pare notificações com um simples clique
- 🖥️ **Daemon multiplataforma**: Executa como serviço no Linux (systemd) e Windows

### Pré-requisitos

- .NET 6.0 ou superior
- Token do Bot do Telegram (obtido do [@BotFather](https://t.me/botfather))

### Instalação

1. **Clone o repositório:**
   ```bash
   git clone https://github.com/imponateado/OnibusBot.git
   cd OnibusBot
   ```

2. **Configure seu token do bot:**
   
   **Opção A: Variável de Ambiente**
   ```bash
   export BOT_TOKEN="seu_token_do_telegram_aqui"
   ```
   
   **Opção B: Arquivo .env**
   Crie um arquivo `.env` na raiz do projeto:
   ```
   BOT_TOKEN=seu_token_do_telegram_aqui
   ```

3. **Compile o projeto:**
   ```bash
   dotnet build
   ```

4. **Execute o bot:**
   ```bash
   dotnet run
   ```

### Executando como Serviço

#### Linux (systemd)

1. **Compile para produção:**
   ```bash
   dotnet publish -c Release -o /opt/onibusbot
   ```

2. **Crie o arquivo de serviço systemd** (`/etc/systemd/system/onibusbot.service`):
   ```ini
   [Unit]
   Description=OnibusBot Telegram Service
   After=network.target

   [Service]
   Type=notify
   ExecStart=/opt/onibusbot/OnibusBot
   Restart=always
   User=onibusbot
   Environment=BOT_TOKEN=seu_token_aqui

   [Install]
   WantedBy=multi-user.target
   ```

3. **Habilite e inicie o serviço:**
   ```bash
   sudo systemctl enable onibusbot
   sudo systemctl start onibusbot
   ```

#### Serviço do Windows

1. **Compile para produção:**
   ```cmd
   dotnet publish -c Release -o C:\OnibusBot
   ```

2. **Instale como Serviço do Windows:**
   ```cmd
   sc create OnibusBot binPath="C:\OnibusBot\OnibusBot.exe"
   sc start OnibusBot
   ```

### Como Usar

1. **Inicie uma conversa** com seu bot no Telegram
2. **Envie `/start`** ou diga "oi" para começar
3. **Digite um número de linha de ônibus** (ex: "175")
4. **Selecione a linha exata** das opções disponíveis
5. **Escolha a direção**: "IDA" ou "VOLTA"
6. **Receba notificações** a cada 2 minutos com localizações dos ônibus
7. **Pare as notificações** a qualquer momento usando o botão "⌐ Parar notificações"

### Contribuindo

1. Faça um fork do repositório
2. Crie uma branch para sua funcionalidade
3. Faça suas alterações
4. Envie um pull request

### Licença

Este projeto é de código aberto. Verifique o repositório para detalhes da licença.

### Suporte

Se você encontrar problemas ou tiver sugestões, por favor abra uma issue no [repositório do GitHub](https://github.com/imponateado/OnibusBot).