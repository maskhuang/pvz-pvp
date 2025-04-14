const express = require('express');
const bodyParser = require('body-parser');
const cors = require('cors');

const app = express();
const PORT = process.env.PORT || 3000;

// 使用中间件
app.use(bodyParser.urlencoded({ extended: false }));
app.use(bodyParser.json());
app.use(cors());

// 示例登录接口（已有）
app.post('/api/login', (req, res) => {
  const { username, password } = req.body;
  // 简单验证，生产环境下请使用数据库验证及密码加密
  if (username === 'testuser' && password === '123456') {
    res.status(200).json({ success: true, message: "登录成功" });
  } else {
    res.status(401).json({ success: false, message: "用户名或密码错误" });
  }
});

// 新增注册接口
app.post('/api/signup', (req, res) => {
  const { username, password } = req.body;
  
  // 基本字段验证
  if (!username || !password) {
    return res.status(400).json({ success: false, message: "用户名和密码不能为空" });
  }
  
  // 这里仅做模拟处理
  // 注意：实际项目中，此处应查询数据库判断用户名是否重复，然后将加密后的密码存入数据库
  
  // 例如模拟不存在重复，则返回注册成功
  console.log(`注册账号：${username}`);
  res.status(200).json({ success: true, message: "注册成功" });
});

app.listen(PORT, () => {
  console.log(`Server is running on port ${PORT}`);
});
