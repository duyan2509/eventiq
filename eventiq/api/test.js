export default async function handler(req, res) {
  console.error('=== TEST ENDPOINT CALLED ===');
  console.error('Method:', req.method);
  console.error('URL:', req.url);
  console.error('Query:', req.query);
  console.error('Body:', req.body);
  console.error('Headers:', req.headers);
  
  return res.status(200).json({
    success: true,
    message: 'Test endpoint works!',
    method: req.method,
    url: req.url,
    hasBody: !!req.body,
    bodyType: typeof req.body,
    timestamp: new Date().toISOString()
  });
}

