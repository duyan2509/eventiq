import React from 'react';
import { Card, Row, Col, Typography, Button, Space } from 'antd';
import { CalendarOutlined, TeamOutlined, TrophyOutlined, StarOutlined } from '@ant-design/icons';
import CreateOrgModal from '../components/Auth/CreateOrgModal';
import { organizationAPI } from '../services/api';
import { useAuth } from '../contexts/AuthContext';
import { useMessage } from '../hooks/useMessage';

const { Title, Paragraph } = Typography;

const Home = () => {
  const features = [
    {
      icon: <CalendarOutlined className="text-5xl text-blue-500" />,
      title: 'Event Management',
      description: 'Create and manage events easily with a user-friendly interface',
    },
    {
      icon: <TeamOutlined className="text-5xl text-green-500" />,
      title: 'Participant Management',
      description: 'Track and manage event participant lists efficiently',
    },
    {
      icon: <TrophyOutlined className="text-5xl text-yellow-500" />,
      title: 'Analytics & Reports',
      description: 'View detailed reports about events and organizational performance',
    },
    {
      icon: <StarOutlined className="text-5xl text-red-500" />,
      title: 'Quality Assessment',
      description: 'Collect feedback and ratings from event participants',
    },
  ];

  const [orgModalVisible, setOrgModalVisible] = React.useState(false);
  const { user, loading, login, setToken } = useAuth();
  const { warning,info, contextHolder } = useMessage();

  const hasOrgRole = user && user.roles && (Array.isArray(user.roles) ? user.roles.includes('Org') : user.roles === 'Org');

  const handleCreateEventClick = () => {
    if (!hasOrgRole) {
      info('You need to create organization before creating event!');
      setOrgModalVisible(true);
      return;
    }
    // ...logic tạo event...
  };

  const handleOrgSuccess = (jwt) => {
    setOrgModalVisible(false);
    if (jwt) setToken(jwt);
  };

  return (
    <div className=" mx-auto">
      {contextHolder}
      {/* Hero Section */}
      <div className="text-center mb-16 py-12">
        <Title level={1} className="text-5xl mb-6">
          Welcome to EventIQ
        </Title>
        <Paragraph className="text-lg text-gray-600 max-w-2xl mx-auto">
          A comprehensive event management platform that helps you organize and manage events professionally
        </Paragraph>
        <Space size="large" className="mt-8">
          <Button type="primary" size="large" className="h-12 px-8 rounded-lg bg-gradient-to-r from-blue-500 to-purple-600 border-none hover:from-blue-600 hover:to-purple-700" onClick={handleCreateEventClick}>
            Create New Event
          </Button>
          <Button size="large" className="h-12 px-8 rounded-lg">
            Explore Events
          </Button>
        </Space>
      </div>

      {/* Features Section */}
      <div className="mb-16">
        <Title level={2} className="text-center mb-12">
          Key Features
        </Title>
        <Row gutter={[24, 24]}>
          {features.map((feature, index) => (
            <Col xs={24} sm={12} lg={6} key={index}>
              <Card
                hoverable
                className="h-full text-center rounded-xl shadow-lg hover:shadow-xl transition-all duration-300 hover:-translate-y-1"
              >
                <div className="mb-4">
                  {feature.icon}
                </div>
                <Title level={4} className="mb-3">
                  {feature.title}
                </Title>
                <Paragraph className="text-gray-600 m-0">
                  {feature.description}
                </Paragraph>
              </Card>
            </Col>
          ))}
        </Row>
      </div>

      {/* Stats Section */}
      <div className="bg-gradient-to-r from-indigo-500 to-purple-600 rounded-2xl p-12 text-center text-white mb-16">
        <Title level={2} className="text-white mb-8">
          Platform Statistics
        </Title>
        <Row gutter={[32, 32]}>
          <Col xs={12} sm={6}>
            <div>
              <Title level={1} className="text-white m-0 text-5xl">
                1000+
              </Title>
              <Paragraph className="text-white/80 m-0">
                Events Organized
              </Paragraph>
            </div>
          </Col>
          <Col xs={12} sm={6}>
            <div>
              <Title level={1} className="text-white m-0 text-5xl">
                50K+
              </Title>
              <Paragraph className="text-white/80 m-0">
                Participants
              </Paragraph>
            </div>
          </Col>
          <Col xs={12} sm={6}>
            <div>
              <Title level={1} className="text-white m-0 text-5xl">
                500+
              </Title>
              <Paragraph className="text-white/80 m-0">
                Trusted Organizations
              </Paragraph>
            </div>
          </Col>
          <Col xs={12} sm={6}>
            <div>
              <Title level={1} className="text-white m-0 text-5xl">
                99%
              </Title>
              <Paragraph className="text-white/80 m-0">
                Satisfaction Rate
              </Paragraph>
            </div>
          </Col>
        </Row>
      </div>

      {/* CTA Section */}
      <div className="text-center py-12">
        <Title level={2} className="mb-4">
          Ready to Get Started?
        </Title>
        <Paragraph className="text-base text-gray-600 mb-8">
          Join thousands of organizations using EventIQ to manage their events
        </Paragraph>
        <Button type="primary" size="large" className="h-12 px-8 rounded-lg bg-gradient-to-r from-blue-500 to-purple-600 border-none hover:from-blue-600 hover:to-purple-700" onClick={() => setOrgModalVisible(true)}>
          Create Organization
        </Button>
      </div>

      <CreateOrgModal
        visible={orgModalVisible}
        onCancel={() => setOrgModalVisible(false)}
        onSuccess={handleOrgSuccess}
      />
    </div>
  );
};

export default Home;
