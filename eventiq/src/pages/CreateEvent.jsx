import React, { useEffect, useState } from 'react';
import { Form, Input, Select, Button, Upload, Alert, Image } from 'antd';
import { PlusOutlined } from '@ant-design/icons';
import { useNavigate } from 'react-router-dom';
import { Editor, EditorState, RichUtils, convertToRaw } from 'draft-js';
import 'draft-js/dist/Draft.css';
import { organizationAPI, eventAPI } from '../services/api';
import { useMessage } from '../hooks/useMessage';
import axios from 'axios';

const styleMap = {
    'BOLD': { fontWeight: 'bold' },
    'ITALIC': { fontStyle: 'italic' },
    'UNDERLINE': { textDecoration: 'underline' },
};

const CreateEvent = () => {
    const [form] = Form.useForm();
    const navigate = useNavigate();
    const { success, error, contextHolder } = useMessage();
    const [loading, setLoading] = useState(false);
    const [organizations, setOrganizations] = useState([]);
    const [provinces, setProvinces] = useState([]);
    const [communes, setCommunes] = useState([]);
    const [selectedProvince, setSelectedProvince] = useState(null);
    const [bannerFile, setBannerFile] = useState(null);
    const [bannerPreview, setBannerPreview] = useState(null);
    const [editorState, setEditorState] = useState(() => EditorState.createEmpty());

    const handleKeyCommand = (command) => {
        const newState = RichUtils.handleKeyCommand(editorState, command);
        if (newState) {
            setEditorState(newState);
            return 'handled';
        }
        return 'not-handled';
    };

    const onStyleClick = (style) => {
        setEditorState(RichUtils.toggleInlineStyle(editorState, style));
    };

    // Load organizations
    useEffect(() => {
        const fetchOrgs = async () => {
            try {
                const res = await organizationAPI.getMyOrganizations();
                setOrganizations(res);
            } catch (err) {
                error('Failed to load organizations');
            }
        };
        fetchOrgs();
    }, []);

    // Load provinces
    useEffect(() => {
        const fetchProvinces = async () => {
            try {
                const res = await axios.get('https://production.cas.so/address-kit/2025-07-01/provinces');
                setProvinces(res.data.provinces);
            } catch (err) {
                error('Failed to load provinces');
            }
        };
        fetchProvinces();
    }, []);

    // Load communes when province changes
    useEffect(() => {
        if (selectedProvince) {
            const fetchCommunes = async () => {
                try {
                    const res = await axios.get(`https://production.cas.so/address-kit/2025-07-01/provinces/${selectedProvince}/communes`);
                    setCommunes(res.data.communes);
                } catch (err) {
                    error('Failed to load communes');
                }
            };
            fetchCommunes();
        } else {
            setCommunes([]);
        }
    }, [selectedProvince]);

    const handleProvinceChange = (value) => {
        setSelectedProvince(value);
        form.setFieldValue('communeCode', undefined);
    };

    const handleBannerChange = (info) => {
        const file = info.fileList.length > 0 ? info.fileList[info.fileList.length - 1].originFileObj : null;
        if (file) {
            setBannerFile(file);
            setBannerPreview(URL.createObjectURL(file));
        } else {
            setBannerFile(null);
            setBannerPreview(null);
        }
    };

    const handleBannerRemove = () => {
        setBannerFile(null);
        setBannerPreview(null);
    };

    const handleSubmit = async (values) => {
        setLoading(true);
        try {
            // Convert editor content to raw JSON string
            const contentState = editorState.getCurrentContent();
            const rawContent = convertToRaw(contentState);
            const description = JSON.stringify(rawContent);

            const eventData = {
                name: values.name,
                description: description,
                banner: bannerFile,
                organizationId: values.organizationId,
                eventAddress: {
                    provinceCode: values.provinceCode,
                    communeCode: values.communeCode,
                    provinceName: provinces.find(p => p.code === values.provinceCode)?.name,
                    communeName: communes.find(c => c.code === values.communeCode)?.name,
                    detail: values.addressDetail
                }
            };

            const res = await eventAPI.create(eventData);
            success('Event created successfully!');
            // Backend now returns Id; fallback to id/eventId if naming differs
            const newEventId = res?.id || res?.eventId || res?.eventID;
            if (newEventId) {
                navigate(`/org/${values.organizationId}/event/${newEventId}`);
            } else {
                // If no id returned, stay and warn
                error('Event created but missing event id from response');
            }
        } catch (err) {
            error(err?.response?.data?.message || 'Failed to create event');
        } finally {
            setLoading(false);
        }
    };

    return (
        <div className="max-w-4xl mx-auto py-8 px-4">
            {contextHolder}
            <h1 className="text-2xl font-bold mb-6">Create New Event</h1>
            <Form
                form={form}
                layout="vertical"
                onFinish={handleSubmit}
                requiredMark={false}
            >
                <Form.Item
                    name="organizationId"
                    label="Organization"
                    rules={[{ required: true, message: 'Please select an organization!' }]}
                >
                    <Select placeholder="Select organization">
                        {organizations.map(org => (
                            <Select.Option key={org.id} value={org.id}>
                                {org.name}
                            </Select.Option>
                        ))}
                    </Select>
                </Form.Item>

                <Form.Item
                    name="name"
                    label="Event Name"
                    rules={[{ required: true, message: 'Please input event name!' }]}
                >
                    <Input placeholder="Enter event name" />
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

                <div className="mb-4">
                    <label className="block mb-2 font-medium">Banner Image</label>
                    <Upload
                        listType="picture-card"
                        showUploadList={false}
                        beforeUpload={() => false}
                        onChange={handleBannerChange}
                        onRemove={handleBannerRemove}
                        accept="image/*"
                        maxCount={1}
                    >
                        {!bannerPreview ? (
                            <div>
                                <PlusOutlined />
                                <div style={{ marginTop: 8 }}>Upload</div>
                            </div>
                        ) : (
                            <Image
                                src={bannerPreview}
                                alt="banner preview"
                                style={{ width: '100%', height: '100%', objectFit: 'cover' }}
                                preview={false}
                            />
                        )}
                    </Upload>
                </div>

                <Form.Item
                    name="provinceCode"
                    label="Province"
                    rules={[{ required: true, message: 'Please select a province!' }]}
                >
                    <Select
                        placeholder="Select province"
                        onChange={handleProvinceChange}
                        showSearch
                        optionFilterProp="children"
                    >
                        {provinces.map(province => (
                            <Select.Option key={province.code} value={province.code}>
                                {province.name}
                            </Select.Option>
                        ))}
                    </Select>
                </Form.Item>

                <Form.Item
                    name="communeCode"
                    label="Commune"
                    rules={[{ required: true, message: 'Please select a commune!' }]}
                >
                    <Select
                        placeholder={selectedProvince ? "Select commune" : "Please select a province first"}
                        disabled={!selectedProvince}
                        showSearch
                        optionFilterProp="children"
                    >
                        {communes.map(commune => (
                            <Select.Option key={commune.code} value={commune.code}>
                                {commune.name}
                            </Select.Option>
                        ))}
                    </Select>
                </Form.Item>

                <Form.Item
                    name="addressDetail"
                    label="Address Detail"
                    rules={[{ required: true, message: 'Please input address detail!' }]}
                >
                    <Input.TextArea placeholder="Enter detailed address" />
                </Form.Item>

                <Form.Item>
                    <Button 
                        type="primary"
                        htmlType="submit"
                        loading={loading}
                        size="large"
                        className="w-full h-12 rounded-lg bg-gradient-to-r from-blue-500 to-purple-600 border-none hover:from-blue-600 hover:to-purple-700"
                    >
                        Create Event
                    </Button>
                </Form.Item>
            </Form>
        </div>
    );
};

export default CreateEvent;