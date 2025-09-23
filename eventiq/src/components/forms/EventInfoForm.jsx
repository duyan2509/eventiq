import React, { useState } from 'react';
import { Form, Input, Button, Card, Upload, message } from 'antd';
import { UploadOutlined } from '@ant-design/icons';
import { Editor } from 'draft-js';
import 'draft-js/dist/Draft.css';

const styleMap = {
    'BOLD': { fontWeight: 'bold' },
    'ITALIC': { fontStyle: 'italic' },
    'UNDERLINE': { textDecoration: 'underline' },
};

const EventInfoForm = ({ 
    form, 
    loading, 
    editorState,
    setEditorState,
    handleKeyCommand,
    onStyleClick,
    onFinish,
    initialBannerUrl 
}) => {
    const [fileList, setFileList] = useState(initialBannerUrl ? [
        {
            uid: '-1',
            name: 'Current banner',
            status: 'done',
            url: initialBannerUrl,
        }
    ] : []);
    return (
        <Card id="event-info" className="mb-8">
            <h2 className="text-xl font-semibold mb-4">Event Information</h2>
            <Form
                form={form}
                layout="vertical"
                onFinish={onFinish}
            >
                <Form.Item
                    name="name"
                    label="Event Name"
                    rules={[{ required: true }]}
                >
                    <Input />
                </Form.Item>

                <Form.Item
                    name="banner"
                    label="Event Banner"
                    valuePropName="fileList"
                    getValueFromEvent={(e) => {
                        if (Array.isArray(e)) {
                            return e;
                        }
                        return e?.fileList;
                    }}
                >
                    <Upload
                        accept="image/*"
                        listType="picture"
                        maxCount={1}
                        fileList={fileList}
                        onChange={({ fileList }) => setFileList(fileList)}
                        beforeUpload={(file) => {
                            const isImage = file.type.startsWith('image/');
                            if (!isImage) {
                                message.error('You can only upload image files!');
                            }
                            const isLt2M = file.size / 1024 / 1024 < 2;
                            if (!isLt2M) {
                                message.error('Image must be smaller than 2MB!');
                            }
                            return isImage && isLt2M;
                        }}
                    >
                        <Button icon={<UploadOutlined />}>Upload Banner</Button>
                    </Upload>
                </Form.Item>

                <Form.Item
                    label="Description"
                >
                    <div className="border rounded-lg p-4">
                        <div className="mb-2 flex gap-2">
                            <Button 
                                size="small"
                                onClick={() => onStyleClick('BOLD')}
                            >
                                Bold
                            </Button>
                            <Button 
                                size="small"
                                onClick={() => onStyleClick('ITALIC')}
                            >
                                Italic
                            </Button>
                            <Button 
                                size="small"
                                onClick={() => onStyleClick('UNDERLINE')}
                            >
                                Underline
                            </Button>
                        </div>
                        <div className="border p-2 min-h-[200px]">
                            <Editor
                                editorState={editorState}
                                onChange={setEditorState}
                                handleKeyCommand={handleKeyCommand}
                                customStyleMap={styleMap}
                                placeholder="Enter event description..."
                            />
                        </div>
                    </div>
                </Form.Item>

                <Form.Item>
                    <Button type="primary" htmlType="submit" loading={loading}>
                        Update Event Information
                    </Button>
                </Form.Item>
            </Form>
        </Card>
    );
};

export default EventInfoForm;