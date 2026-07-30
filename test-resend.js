const fs = require('fs');
const path = require('path');

const args = process.argv.slice(2);

if (args.length < 1) {
  console.error("Использование: node test-resend.js <email_получателя>");
  process.exit(1);
}

const toEmail = args[0];

// Читаем .env файл напрямую
const envPath = path.join(__dirname, '.env');
if (!fs.existsSync(envPath)) {
  console.error("❌ Файл .env не найден в корне проекта.");
  process.exit(1);
}

const envFile = fs.readFileSync(envPath, 'utf8');
const env = {};
envFile.split('\n').forEach(line => {
  const match = line.match(/^([^#=]+)=(.*)$/);
  if (match) {
    env[match[1].trim()] = match[2].trim();
  }
});

// Берем ключ из SMTP_PASSWORD и адрес отправителя из SMTP_ADDRESS
const apiKey = env['SMTP_PASSWORD'];
const fromAddress = env['SMTP_ADDRESS'] || 'noreply@send.polyraspad.online';
const displayName = env['SMTP_DISPLAY_NAME'] || 'Polyraspad';

if (!apiKey || !apiKey.startsWith('re_')) {
  console.error(`❌ В файле .env не найден валидный ключ Resend в переменной SMTP_PASSWORD.`);
  console.error(`Текущее значение: ${apiKey}`);
  console.error(`Убедись, что ты вставил туда настоящий ключ (начинается на 're_').`);
  process.exit(1);
}

async function sendTestEmail() {
  console.log(`Используем ключ Resend из .env...`);
  console.log(`Отправитель: ${displayName} <${fromAddress}>`);
  console.log(`Отправка тестового письма на ${toEmail}...`);

  try {
    const response = await fetch('https://api.resend.com/emails', {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${apiKey}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({
        from: `${displayName} <${fromAddress}>`,
        to: [toEmail],
        subject: 'Polyraspad: Test Email via Resend',
        html: '<strong>It works!</strong><br><p>This is a test email sent from the Polyraspad project using the Resend API. Ключ успешно прочитан из .env файла!</p>'
      })
    });

    if (response.ok) {
      const data = await response.json();
      console.log('\n✅ Письмо успешно отправлено!');
      console.log('ID письма:', data.id);
      console.log(`Проверь статус доставки в панели управления: https://resend.com/emails`);
    } else {
      const errorText = await response.text();
      let errorData;
      try {
        errorData = JSON.parse(errorText);
      } catch (e) {
        errorData = errorText;
      }
      console.error('\n❌ Ошибка при отправке от API Resend:');
      console.error(errorData);
      
      if (response.status === 403) {
        console.error('\nПодсказка: Проверь, что домен из переменной SMTP_ADDRESS верифицирован в Resend, и API ключ имеет к нему доступ.');
      }
    }
  } catch (err) {
    console.error('\n❌ Внутренняя ошибка при выполнении запроса:', err.message);
  }
}

sendTestEmail();
